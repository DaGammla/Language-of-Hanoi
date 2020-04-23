using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Language_of_Hanoi {
	class Interpreter {

		//All functions that got defined
		//Key: The Name Of The Function
		//Value: A HanoiFunction instance, which holds the instructions that this function executes
		static Dictionary<string, HanoiFunction> functions = new Dictionary<string, HanoiFunction>();

		//The Hanoi Instance that holds all the Stacks with all memory values
		static Hanoi hanoi = new Hanoi();

		//Contains the Slot (S) value
		static ReturnValue slot = new ReturnValue();

		//Whether a CASE instruction got performed and an ELSE instruction can now be performed
		bool canElseTrigger = false;

		//Hold the outcome of the last condition test perfomed by an CASE instruction
		//Stored for ELSE instructions
		bool lastCaseCondition = true;


		static void Main(string[] args) {

			//Contains all Lines from the File from the Arguments
			string[] mainLines = { };

			//The path that is provided by the arguments
			string path = "";

			//Checks whether an argument is provided
			if (args.Length == 0) {
				ThrowError("Argument Error: a path has to be provided as an argument");
			} else {

				try {
					//Combines all arguments together as they are automatically split by blank spaces
					//Done because paths can contain blank spaces
					foreach (string part in args) {
						path += " " + part;
					}
					//Some adjustments
					path = path.Trim();
					path = path.Replace("\"", "");

					//Reads the File from the Arguments
					mainLines = File.ReadAllLines(path);
					
				} catch {
					//Something went wrong when reading the file
					ThrowError("Access Error: could not open file : \"" + path + "\"");
				}

			}

			//The Main Interpreter, that runs the code of the main file
			Interpreter interp = new Interpreter();
			
			//Execute the main file and 
			ReturnValue exitVal = interp.ExecuteLines(mainLines, path);

			Console.WriteLine();

			//Uses the Interpreters Exit Value als real exit value, if there is one
			if (exitVal.HasValue()) {
				Environment.Exit(exitVal.GetValue());
			} else {
				Environment.Exit(0);
			}
			
		}


		ReturnValue ExecuteLines(string[] lines, string fileName, int fileOffset = 1) {
			for (int i = 0; i < lines.Length; i++) {
				string[] instAndPars = ExtractInstructionAndParameters(lines[i]);
				string instruction = instAndPars[0];
				string parameter = instAndPars[1];
				int lineId = fileOffset + i;

				switch (instruction) {
					case "": {
						continue;
					}
					case "DUMP": {

						if (parameter != "") {
							ThrowAllowedParameterException(instruction, fileName, lineId);
						}

						slot = new ReturnValue();
						continue;
					}
					case "STOW": {

						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}
						if (parameter == "A") {
							hanoi.PushA(slot.GetValue());
						} else if (parameter == "B") {
							hanoi.PushB(slot.GetValue());
						} else if (parameter == "C") {
							hanoi.PushC(slot.GetValue());
						} else {
							ThrowParameterException(instruction, parameter, fileName, lineId);
						}
						slot = new ReturnValue();
						continue;
					}
					case "COPY": {

						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);

						if (slot.HasValue()) {
							ThrowFullSlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(num);

						continue;
					}
					case "TAKE": {

						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);

						if (slot.HasValue()) {
							ThrowFullSlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId, true);

						slot = new ReturnValue(num);

						continue;
					}

					case "TYPE": {

						if (parameter != "") {
							if (parameter.StartsWith('"') && parameter.EndsWith('"') && parameter.Length > 2) {
								try {
									Console.Write(Regex.Unescape(parameter.Substring(1, parameter.Length - 2)));
								} catch (Exception e){
									Console.Write(parameter.Substring(1, parameter.Length - 2));
								}
							} else {
								ThrowParameterException(instruction, parameter, fileName, lineId);
							}

							continue;
						}

						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int slotValue = slot.GetValue();

						if (slotValue <= 65535 && slotValue >= 0) {

							char character = Convert.ToChar(slotValue);
							Console.Out.Write(character);

						} else {
							ThrowInvalidCharacterException(slotValue, fileName, lineId);
						}
						continue;
					}

					case "GIVE": {

						if (parameter != "") {
							ThrowAllowedParameterException(instruction, fileName, lineId);
						}

						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						Console.Out.Write(slot.GetValue());

						continue;
					}

					case "READ": {

						if (parameter != "") 
							ThrowParameterException(instruction, parameter, fileName, lineId);

						if (slot.HasValue()) {
							ThrowFullSlotException(instruction, fileName, lineId);
						}

						slot = new ReturnValue(Convert.ToInt32(Console.ReadKey().KeyChar));

						continue;
					}

					case "CASE": {

						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);

						int corredpondingSeal = findClosingSeal(lines, i);
						if (corredpondingSeal <= 0) {
							ThrowNoSealException(instruction, fileName, lineId);
						}

						lastCaseCondition = TestCondition(parameter, fileName, lineId);

						if (lastCaseCondition) {
							Interpreter subInterpreter = new Interpreter();
							string[] subLines = lines.SubArray(i + 1, corredpondingSeal - 1);
							ReturnValue retVal = subInterpreter.ExecuteLines(subLines, fileName, lineId + 1);
							if (retVal is ExitValue || retVal is SkipValue)
								return retVal;

						}

						i = corredpondingSeal;

						canElseTrigger = true;

						continue;
					}

					case "ELSE": {

						bool hasCaseTest = false;
						string caseParameter = "";

						if (parameter != "") {
							string[] caseStuff = ExtractInstructionAndParameters(parameter);

							string caseInstruction = caseStuff[0];

							if (caseInstruction == "CASE") {
								hasCaseTest = true;
								caseParameter = caseStuff[1];

								if (caseParameter == "")
									ThrowParameterException(caseInstruction, caseParameter, fileName, lineId);
							} else {
								ThrowParameterException(instruction, parameter, fileName, lineId);
							}
						}
							

						if (canElseTrigger == false) {
							ThrowEarlyElseException(fileName, lineId);
						}

						int corredpondingSeal = findClosingSeal(lines, i);

						if (corredpondingSeal <= 0) {
							ThrowNoSealException(instruction, fileName, lineId);
						}

						if (!lastCaseCondition && (!hasCaseTest || TestCondition(caseParameter, fileName, lineId))) {
							if (hasCaseTest)
								lastCaseCondition = true;
							Interpreter subInterpreter = new Interpreter();
							string[] subLines = lines.SubArray(i + 1, corredpondingSeal - 1);
							ReturnValue retVal = subInterpreter.ExecuteLines(subLines, fileName, lineId + 1);
							if (retVal is ExitValue || retVal is SkipValue)
								return retVal;
						}

						i = corredpondingSeal;

						continue;
					}

					case "LOOP": {

						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);

						int corredpondingSeal = findClosingSeal(lines, i);
						if (corredpondingSeal <= 0) {
							ThrowNoSealException(instruction, fileName, lineId);
						}

						while (TestCondition(parameter, fileName, lineId)) {
							Interpreter subInterpreter = new Interpreter();
							string[] subLines = lines.SubArray(i + 1, corredpondingSeal - 1);
							ReturnValue retVal = subInterpreter.ExecuteLines(subLines, fileName, lineId + 1);
							if (retVal is ExitValue) {
								break;
							}
						}

						i = corredpondingSeal;

						canElseTrigger = true;

						continue;
					}

					case "SKIP": {
						if (parameter != "") {
							ThrowAllowedParameterException(instruction, fileName, lineId);
						}
						return new SkipValue();
					}

					case "EXIT": {

						if (parameter != "") {
							return new ExitValue(GetNumber(parameter, instruction, fileName, lineId));
						} else {
							return new ExitValue();
						}
					}

					case "FUNC": {

						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);

						if (parameter.Contains('\'') || parameter.Contains('"'))
							ThrowFunctionHeaderException(parameter, fileName, lineId);

						int corredpondingSeal = findClosingSeal(lines, i);
						if (corredpondingSeal <= 0) {
							ThrowNoSealException(instruction, fileName, lineId);
						}
						string[] subLines = lines.SubArray(i + 1, corredpondingSeal - 1);
						functions.Add(parameter, new HanoiFunction(subLines, fileName, lineId));

						i = corredpondingSeal;

						continue;
					}

					case "EXEC": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);


						if (functions.ContainsKey(parameter)) {
							Interpreter subInterpreter = new Interpreter();
							HanoiFunction func = functions[parameter];
							ReturnValue retVal = subInterpreter.ExecuteLines(func.GetLines(), func.GetFileName(), func.GetLine() + 1);
							if (retVal is ExitValue && retVal.HasValue()) {

								if (slot.HasValue()) {
									ThrowFullSlotException(instruction, fileName, lineId);
								}

								slot = new ReturnValue(retVal.GetValue());
							}
						} else {
							ThrowFunctionException(parameter, fileName, lineId);
						}
						continue;
					}

					case "TURN": {
						if (parameter == "") {
							hanoi.Turn(1);
						} else {
							hanoi.Turn(GetNumber(parameter, instruction, fileName, lineId));
						}
						continue;
					}

					case "WAIT": {
						if(parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);

						int sleepTime = GetNumber(parameter, instruction, fileName, lineId);
						if (sleepTime >= 0) {
							Thread.Sleep(sleepTime);
						} else {
							ThrowWaitException(sleepTime, fileName, lineId);
						}

						continue;
					}

					case "OPEN": {
						if (parameter.Length > 2 && parameter.StartsWith('"') && parameter.EndsWith('"')) {
							string path = parameter.Substring(1, parameter.Length - 2);

							try {
								ReturnValue retVal;
								FileInfo file = new FileInfo(path);
								if (file.Exists) {
									//directory = file.Directory.FullName;

									string[] subLines = File.ReadAllLines(path);
									Interpreter subInterpreter = new Interpreter();
									retVal = subInterpreter.ExecuteLines(subLines, fileName, lineId + 1);

								} else {

									path = Regex.Replace(new FileInfo(fileName).Directory.FullName + "/" + path, @"[\\/]+", "/");

									string[] subLines = File.ReadAllLines(path);
									Interpreter subInterpreter = new Interpreter();
									retVal = subInterpreter.ExecuteLines(subLines, fileName, lineId + 1);
								}

								if (retVal is ExitValue && retVal.HasValue()) {

									if (slot.HasValue()) {
										ThrowFullSlotException(instruction, fileName, lineId);
									}

									slot = new ReturnValue(retVal.GetValue());
								}

							} catch {
								ThrowErrorEase("Access Error: could not open file : \"" + path + "\"", fileName, lineId);
							}

						} else {
							ThrowParameterException(instruction, parameter, fileName, lineId);
						}

						continue;
					}
					
					case "FILE": {
						if (parameter.Length > 2 && parameter.StartsWith('"') && parameter.EndsWith('"')) {
							string path = parameter.Substring(1, parameter.Length - 2);

							int corredpondingSeal = findClosingSeal(lines, i);
							if (corredpondingSeal <= 0) {
								ThrowNoSealException(instruction, fileName, lineId);
							}

							try {
								string fileContents;
								FileInfo file = new FileInfo(path);
								if (file.Exists) {

									fileContents = File.ReadAllText(path);

								} else {

									path = Regex.Replace(new FileInfo(fileName).Directory.FullName + "/" + path, @"[\\/]+", "/");

									fileContents = File.ReadAllText(path);
								}

								
								string[] subLines = lines.SubArray(i + 1, corredpondingSeal - 1);

								for (int j = 0; j < fileContents.Length; j++) {
									char c = fileContents[j];

									if (slot.HasValue())
										ThrowFullFileSlotException(fileName, lineId);

									slot = new ReturnValue(Convert.ToInt32(c));

									Interpreter subInterpreter = new Interpreter();
									ReturnValue retValue = subInterpreter.ExecuteLines(subLines, fileName, lineId + 1);
									if (retValue is ExitValue)
										break;
								}

								i = corredpondingSeal;

							} catch {
								ThrowErrorEase("Access Error: could not open file : \"" + path + "\"", fileName, lineId);
							}

						} else {
							ThrowParameterException(instruction, parameter, fileName, lineId);
						}

						continue;
					}

					case "ADD": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() + num);

						continue;
					}

					case "SUB": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() - num);

						continue;
					}

					case "MUL": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() * num);

						continue;
					}

					case "DIV": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() / num);

						continue;
					}

					case "REM": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() % num);

						continue;
					}

					case "POW": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue((int)Math.Pow(slot.GetValue(), num));

						continue;
					}

					case "SHL": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						if (num < 0) {
							ThrowParameterException(instruction, parameter, fileName, lineId);
						}

						slot = new ReturnValue(slot.GetValue() << num);

						continue;
					}

					case "SHR": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						if (num < 0) {
							ThrowParameterException(instruction, parameter, fileName, lineId);
						}

						slot = new ReturnValue(slot.GetValue() >> num);

						continue;
					}

					case "ABS": {
						if (parameter != "")
							ThrowAllowedParameterException(instruction, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						slot = new ReturnValue(slot.GetValue() > 0 ? slot.GetValue() : -slot.GetValue());

						continue;
					}

					case "AND": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() & num);

						continue;
					}

					case "XOR": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() ^ num);

						continue;
					}

					case "BOR": {
						if (parameter == "")
							ThrowParameterException(instruction, parameter, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						int num = GetNumber(parameter, instruction, fileName, lineId);

						slot = new ReturnValue(slot.GetValue() | num);

						continue;
					}

					case "NOT": {
						if (parameter != "")
							ThrowAllowedParameterException(instruction, fileName, lineId);
						if (!slot.HasValue()) {
							ThrowEmptySlotException(instruction, fileName, lineId);
						}

						slot = new ReturnValue(~slot.GetValue());

						continue;
					}

					default: {
						ThrowInstructionException(instruction, fileName, lineId);
						continue;			
					}
				}
			}

			return new ReturnValue();
		}

		static Operator[] operators = {
			new Operator("==", (a, b) => { return a == b; }),
			new Operator("!=", (a, b) => { return a != b; }),
			new Operator("<=", (a, b) => { return a <= b; }),
			new Operator(">=", (a, b) => { return a >= b; }),
			new Operator("<", (a, b) => { return a < b; }),
			new Operator(">", (a, b) => { return a > b; })
			
		};

		bool TestCondition(string condition, string fileName, int line) {
			string conditionClear = condition.Trim();
			foreach (Operator op in operators) {
				if (conditionClear.Contains(op.GetLook())) {
					string[] conSplit = conditionClear.Split(op.GetLook());
					if (conSplit.Length == 2) {
						int a = GetNumber(conSplit[0].Trim(), condition, fileName, line);
						int b = GetNumber(conSplit[1].Trim(), condition, fileName, line);
						return op.Test(a, b);
					} else {
						ThrowInvalidOperatorException(op.GetLook(), condition, fileName, line);
					}
				}
			}

			if (condition == "A")
				return hanoi.PeekA().HasValue();
			if (condition == "B")
				return hanoi.PeekB().HasValue();
			if (condition == "C")
				return hanoi.PeekC().HasValue();
			if (condition == "S")
				return slot.HasValue();

			if (condition == "!A")
				return !hanoi.PeekA().HasValue();
			if (condition == "!B")
				return !hanoi.PeekB().HasValue();
			if (condition == "!C")
				return !hanoi.PeekC().HasValue();
			if (condition == "!S")
				return !slot.HasValue();

			if (condition.ToUpper() == "EVER")
				return true;

			ThrowInvalidConditionException(condition, fileName, line);

			return false;
		}



		int GetNumber(string input, string usage, string fileName, int line, bool pop = false) {
			int val;

			string unescaped = input;

			try {
				unescaped = Regex.Unescape(input);
			} catch (Exception e){
				ThrowNumberException(input, usage, fileName, line);
			}

			if (int.TryParse(input, out val)) {
				return val;
			} else if (input == "A") {
				ReturnValue retVal = hanoi.PeekA();
				if (!retVal.HasValue())
					ThrowEmptyStackException(usage, input, fileName, line);
				if (pop)
					hanoi.PopA();

				return retVal.GetValue();
			} else if (input == "B") {
				ReturnValue retVal = hanoi.PeekB();
				if (!retVal.HasValue())
					ThrowEmptyStackException(usage, input, fileName, line);
				if (pop)
					hanoi.PopB();

				return retVal.GetValue();
			} else if (input == "C") {
				ReturnValue retVal = hanoi.PeekC();
				if (!retVal.HasValue())
					ThrowEmptyStackException(usage, input, fileName, line);
				if (pop)
					hanoi.PopC();

				return retVal.GetValue();
			} else if (input == "S") {
				if (!slot.HasValue())
					ThrowEmptySlotException(usage, fileName, line);
				return slot.GetValue();
			} else if (unescaped.Length == 3 && unescaped.StartsWith("'") && unescaped.EndsWith("'")) {
				char character = unescaped.ToCharArray()[1];
				return Convert.ToInt32(character);
			} else if (input.ToUpper().StartsWith("SIZE")) {
				var shortened = Regex.Replace(input, @"\s+", " ");
				if (shortened.Length == 6) {
					if (shortened.EndsWith('A')) {
						return hanoi.SizeA();
					} else if (shortened.EndsWith('B')) {
						return hanoi.SizeB();
					} else if (shortened.EndsWith('C')) {
						return hanoi.SizeC();
					}
				}
			}
			
			ThrowNumberException(input, usage, fileName, line);
			
			return 0;
		}

		static int findClosingSeal(string[] lines, int start) {

			int sealsToClose = 0;

			for (int i = start; i < lines.Length; i++) {
				string line = lines[i];
				string currInstruction = ExtractInstructionAndParameters(line)[0];
				if (doesNeedSeal(currInstruction)) {
					sealsToClose++;
				} else if (currInstruction == "SEAL") {
					sealsToClose--;
					if (sealsToClose == 0)
						return i;
				}
			}

			return -1;
		}

		static string[] sealInstructions = { "CASE", "FUNC", "LOOP", "FILE", "ELSE" };

		static bool doesNeedSeal(string instruction) {
			foreach (string sealInstruction in sealInstructions) {
				if (sealInstruction == instruction)
					return true;
			}

			return false;
		}


		static string[] ExtractInstructionAndParameters(string line) {
			line = line.Trim();
			int comment = line.IndexOf("//");
			if (comment >= 0) {
				line = line.Substring(0, comment);
				line = line.TrimEnd();
			}
			string[] instAndPars = { "", "" };
			int space = line.IndexOf(' ');
			if (space > 0) {
				instAndPars[0] = line.Substring(0, space).ToUpper();
				instAndPars[1] = line.Substring(space + 1).Trim();
			} else {
				instAndPars[0] = line.ToUpper();
			}
			return instAndPars;
		}

		static void ThrowEmptyStackException(string instruction, string stack, string fileName, int line) {
			ThrowErrorEase("Stack Empty Exception: " + instruction + " can not be performed on STACK '" + stack + "' if it is empty. Performed STOW on it first.", fileName, line);
		}

		static void ThrowEmptySlotException(string instruction, string fileName, int line) {
			ThrowErrorEase("Slot Empty Exception: " + instruction + " can not be performed when SLOT is empty. Performed TAKE or COPY first.", fileName, line);
		}

		static void ThrowFullSlotException(string instruction, string fileName, int line) {
			ThrowErrorEase("Slot Not Empty Exception: " + instruction + " can only be performed when SLOT is empty. Performed DUMP or STOW first.", fileName, line);
		}

		static void ThrowFullFileSlotException(string fileName, int line) {
			ThrowErrorEase("Slot Not Empty Exception: at the beginning of FILE instructions SLOT has to be empty. Performed DUMP or STOW first.", fileName, line);
		}

		static void ThrowInstructionException(string instruction, string fileName, int line) {
			ThrowErrorEase("Illegal Instruction Exception: instruction " + instruction + " is not available.", fileName, line);
		}

		static void ThrowParameterException(string instruction, string parameter, string fileName, int line) {
			ThrowErrorEase("Illegal Parameter Exception: parameter \"" + parameter + "\" is illegal for " + instruction + ".", fileName, line);
		}

		static void ThrowNumberException(string number, string usage, string fileName, int line) {
			ThrowErrorEase("Illegal Value Exception: the value \"" + number + "\" used for \"" + usage + "\" is not valid.", fileName, line);
		}

		static void ThrowStringException(string number, string usage, string fileName, int line) {
			ThrowErrorEase("Illegal Value Exception: the value \"" + number + "\" used for \"" + usage + "\" is not valid.", fileName, line);
		}

		static void ThrowEarlyElseException(string fileName, int line) {
			ThrowErrorEase("Early Else Exception: ELSE can not be used unless a CASE has been performed earlier.", fileName, line);
		}

		static void ThrowInvalidOperatorException(string oper, string usage, string fileName, int line) {
			ThrowErrorEase("Invalid Operator Exception: the operator \"" + oper + "\" can not be applied in \"" + usage + "\".", fileName, line);
		}

		static void ThrowInvalidConditionException(string condition, string fileName, int line) {
			ThrowErrorEase("Invalid Condition Exception: the condition \"" + condition + "\" is not a valid condition.", fileName, line);
		}

		static void ThrowInvalidCharacterException(int number, string fileName, int line) {
			ThrowErrorEase("Invalid Character Exception: the character \"" + number + "\" is not a valid character.", fileName, line);
		}

		static void ThrowWaitException(int time, string fileName, int line) {
			ThrowErrorEase("Invalid Wait Exception: is is not possible to wait for " + time + " milliseconds.", fileName, line);
		}

		static void ThrowAllowedParameterException(string instruction, string fileName, int line) {
			ThrowErrorEase("Illegal Parameter Exception: parameters are not allowed for " + instruction + ".", fileName, line);
		}

		static void ThrowNoSealException(string instruction, string fileName, int line) {
			ThrowErrorEase("No Seal Exception: no corresponding SEAL was found for " + instruction + " in line " + line + ".", fileName, line);
		}

		static void ThrowFunctionException(string header, string fileName, int line) {
			ThrowErrorEase("Undefined Function Exception: there is no function defined with the head \"" + header + "\".", fileName, line);
		}

		static void ThrowFunctionHeaderException(string header, string fileName, int line) {
			ThrowErrorEase("Function Header Exception: the function header " + header + " is not allowed.", fileName, line);
		}

		static void ThrowErrorEase(string error, string fileName, int line) {
			ThrowError("hanoi!! - " + error + "\nAt \"" + fileName + "\": " + line);
		}

		static void ThrowError(string error) {
			ConsoleColor color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine();
			Console.WriteLine(error);
			Console.ForegroundColor = color;

			Environment.Exit(-1);
		}
	}

	class Operator {

		private string look;
		private Func<int, int, bool> tester;

		public Operator(string look, Func<int, int, bool> tester) {
			this.look = look;
			this.tester = tester;
		}

		public bool Test(int a, int b) {
			return tester(a, b);
		}

		public string GetLook() {
			return look;
		}
	}

	class ReturnValue {

		private int val = 0;
		private bool hasValue { get; } = false;

		public ReturnValue() {
			hasValue = false;
		}

		public ReturnValue(int val) {
			hasValue = true;
			this.val = val;
		}

		public bool HasValue() {
			return hasValue;
		}

		public int GetValue() {
			if (!hasValue)
				throw new NullReferenceException("No actual return value was given");

			return val;
		}
	}

	class SkipValue : ReturnValue {
		public SkipValue() : base() {
			
		}

		public SkipValue(int val) : base(val) {
			
		}
	}

	class ExitValue : ReturnValue {
		public ExitValue() : base() {

		}

		public ExitValue(int val) : base(val) {

		}
	}

	class HanoiFunction {

		private string[] lines;
		string fileName;
		int line;

		public HanoiFunction(string[] lines, string fileName, int line) {
			this.lines = lines;
			this.fileName = fileName;
			this.line = line;
		}

		public string[] GetLines() {
			return lines;
		}

		public string GetFileName() {
			return fileName;
		}

		public int GetLine() {
			return line;
		}
	}


	class Hanoi {
		private Stack stack0 = new Stack();
		private Stack stack1 = new Stack();
		private Stack stack2 = new Stack();

		private int turn = 0;

		//Changes the correlation between the Letters A, B, C and the Stacks 0, 1, 2
		public void Turn(int amount) {
			turn += amount;
			turn = (turn % 3 + 3) % 3;
		}

		//Push A..C: Pushed a valute to the top of the associated stack
		public void PushA(int val) {
			if (turn == 0) {
				stack0.Push(val);
			} else if (turn == 1) {
				stack1.Push(val);
			} else {
				stack2.Push(val);
			}
		}

		public void PushB(int val) {
			if (turn == 0) {
				stack1.Push(val);
			} else if (turn == 1) {
				stack2.Push(val);
			} else {
				stack0.Push(val);
			}
		}

		public void PushC(int val) {
			if (turn == 0) {
				stack2.Push(val);
			} else if (turn == 1) {
				stack0.Push(val);
			} else {
				stack1.Push(val);
			}
		}


		//Peek A..C: Gets the value on the top of the associated stack without removing it
		public ReturnValue PeekA() {
			if (turn == 0) {
				return Peek0();
			} else if (turn == 1) {
				return Peek1();
			} else {
				return Peek2();
			}
		}

		public ReturnValue PeekB() {
			if (turn == 0) {
				return Peek1();
			} else if (turn == 1) {
				return Peek2();
			} else {
				return Peek0();
			}
		}

		public ReturnValue PeekC() {
			if (turn == 0) {
				return Peek2();
			} else if (turn == 1) {
				return Peek0();
			} else {
				return Peek1();
			}
		}


		//Pop A..C: Gets the value on the top of the associated stack and removes it
		public ReturnValue PopA() {
			if (turn == 0) {
				return Pop0();
			} else if (turn == 1) {
				return Pop1();
			} else {
				return Pop2();
			}
		}

		public ReturnValue PopB() {
			if (turn == 0) {
				return Pop1();
			} else if (turn == 1) {
				return Pop2();
			} else {
				return Pop0();
			}
		}

		public ReturnValue PopC() {
			if (turn == 0) {
				return Pop2();
			} else if (turn == 1) {
				return Pop0();
			} else {
				return Pop1();
			}
		}


		//Size A..C: Gets the size of the associated stack
		public int SizeA() {
			if (turn == 0) {
				return Size0();
			} else if (turn == 1) {
				return Size1();
			} else {
				return Size2();
			}
		}

		public int SizeB() {
			if (turn == 0) {
				return Size1();
			} else if (turn == 1) {
				return Size2();
			} else {
				return Size0();
			}
		}

		public int SizeC() {
			if (turn == 0) {
				return Size2();
			} else if (turn == 1) {
				return Size0();
			} else {
				return Size1();
			}
		}


		//Peek 0..2: Gets the value on the top of the stack without removing it
		private ReturnValue Peek0() {
			if (stack0.Count > 0) {
				return new ReturnValue((int)stack0.Peek());
			}
			return new ReturnValue();
		}
		private ReturnValue Peek1() {
			if (stack1.Count > 0) {
				return new ReturnValue((int)stack1.Peek());
			}
			return new ReturnValue();
		}
		private ReturnValue Peek2() {
			if (stack2.Count > 0) {
				return new ReturnValue((int)stack2.Peek());
			}
			return new ReturnValue();
		}


		//Pop 0..2: Gets the value on the top of the stack and removes it
		private ReturnValue Pop0() {
			if (stack0.Count > 0) {
				return new ReturnValue((int)stack0.Pop());
			}
			return new ReturnValue();
		}
		private ReturnValue Pop1() {
			if (stack1.Count > 0) {
				return new ReturnValue((int)stack1.Pop());
			}
			return new ReturnValue();
		}
		private ReturnValue Pop2() {
			if (stack2.Count > 0) {
				return new ReturnValue((int)stack2.Pop());
			}
			return new ReturnValue();
		}

		//Size 0..2: Gets the size of the stacks
		private int Size0() {
			return stack0.Count;
		}
		private int Size1() {
			return stack1.Count;
		}
		private int Size2() {
			return stack2.Count;
		}

	}

	public static class Utils {
		public static T[] SubArray<T>(this T[] array, int start, int end) {
			int length = end - start + 1;
			T[] subArray = new T[length];
			Array.Copy(array, start, subArray, 0, length);
			return subArray;
		}
	}
}
