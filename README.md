# Language of Hanoi

## Basics
Language of Hanoi is a simple scripting language based on the [Tower of Hanoi Puzzle](https://en.wikipedia.org/wiki/Tower_of_Hanoi). Like in the game, in this scripting language, there are three stacks on which values can be stored, called A, B and C. Another value can also be stored in the SLOT, represented in the script by an S. Many Instructions and all the operators are performed on the value held by the SLOT. 

### Stacks
In this language, you have 3 stacks on which you can store values. Only the last stored value of every stack can be accessed. This is done with either the copy instruction, that copies the top value of the stack to the SLOT (S), or the take instruction, that moves the value away from the stack and onto the SLOT.  
The stacks can be accessed by their identifiers A, B and C. With the turn instruction, the identifiers can be changed around.  
When a stack is empty but is tried to be accessed, it throws a stack empty exception.  

### Slot

### Numbers

### Strings

### Functions

## Conventions

* The script files of this language are usually stored with the "hanoi" extension. Like "script.hanoi".  
* Instructions can be written in uppercase, lowercase and even mixed but should be written in lowercase.  
* Function names can contain special characters and can contain letters of any case. However, the convention is that they do not contain special characters and start with a lowercase character. Words inside the function name should not be seperated by underscores. Every other word inside the function name should begin with an uppercase letter. Like "printEntireStackA".  
  
## Instructions
### DUMP:
Parameters: `None`  
Description:  
Clears SLOT (S). Allows taking values again afterwards. Picking up a value while the SLOT is not empty will result in an exception, so  you have to dump first.  
Example:  
```
take 1
dump
take 2
```

***

### STOW:
Parameters: `A or B or C`  
Description:  
Stores the SLOT (S) value on top of the given Stack, A, B, or C and Clears SLOT (S) in the Process.  
Example:  
```
take 1
stow A
```

***

### COPY:
Parameters: `Number`  
Description:  
Copies the given number to the SLOT (S). If the number is coming from a stack, the stack will be unchanged.  
Example:  
```
copy 1
stow A
copy A
```

***

### TAKE:
Parameters: `Number`  
Description:  
Copies the given number to the SLOT (S). If the number is coming from a stack, then it will be removed from it.  
Example:  
```
take 1
stow A
take A
```

***

### TYPE:
Parameters: `None or String`  
Description:  
Prints the SLOT (S) value as a Unicode Character to the Console if no parameter is give. However, if a string is given as a parameter, it prints that string to the console.  
Example:  
```
copy 'A'
type            //Console Output: A
type "Hanoi"    //Console Output: Hanoi
```

***

### GIVE:
Parameters: `None`  
Description:  
Prints the SLOT (S) value as a base 10 number to the Console.  
Example:  
```
copy 5
give      //Console Output: 5
dump
copy 'A'
give      //Console Output: 65
```

***

### READ:
Parameters: `None`  
Description:  
Waits for a character input in the console and saves it as a number to the SLOT (S).  
Example:  
```
loop ever    //Endless Loop
    read     //Reads one Character from the Console
    type     //Gives it back out
    dump     //Clears slot so it can be filled again
seal         //End of Loop
```

***

### CASE:
Parameters: `Condition`  
Uses seal block  
Description:  
Executes the code between it and its corresponding seal instruction if the given condition turns out to be true. If not, the interpreter continues after the seal instruction.  
Example:  
```
copy 10
case S == 10     //Checks whether SLOT holds the value 10
    type "Slot holds 10"    //Only executed if the Slot value is 10
seal     //End of case
```

***

### ELSE:
Parameters: `None or CASE Instruction`  
Uses seal block  
Description:  
Executes the code between it and its corresponding seal instruction if the condition of the last case instruction turned out to be false. Can be combined with another case instruction to check for another condition.  
Example:  
```
case S == 10     //Checks whether SLOT holds the value 10
    type "Slot holds 10"
seal     //End of case
else case S == 9  //Checks whether SLOT holds the value 9
    type "Slot holds 9"
seal     //End of else case
else  //Executed if none of the before were true
    type "Slot holds something else"
seal     //End of else
```

***

### LOOP:
Parameters: `Condition`  
Uses seal block  
Description:  
Executes the code between it and its corresponding seal instruction while the given condition is still true and the loop is not exited. Afterwards, the interpreter continues after the seal instruction. It can be exited with an exit instruction and single cycles can be skipped with a skip instruction.  
Example:  
```
copy 0
loop S < 10   //While S ist smaller or equal 10
    give       //Prints S as number
    type "\n"  //Type New Line
    add 1      //Higher S by 1
seal           //This Loop prints the digits 0..9 to the console
```

***

### SKIP:
Parameters: `None`  
Description:  
Skips the rest of the instructions of the innermost loop, file instruction function or script file if none of the others is present.  
Example:  
```
copy 0
loop S < 9   //While S ist smaller or equal 10
    add 1      //Higher S by 1
    case S == 5
        skip   //Skips if S == 5
    seal
    give       //Prints S as number
    type "\n"  //Type New Line
seal           //This Loop prints the digits 1..9 without the 5 to the console
```

***

### EXIT:
Parameters: `None or Number`  
Description:  
Exits the innermost loop, file instruction, function or script file if none of the others is present. If a number is given as parameter and the innermost is a function or file, the exit instruction gives it a return value.  
Example:  
```
copy 0
loop ever   //While S ist smaller or equal 10
    case S == 10
        exit   //Exit Loop if S == 10
    seal
    give       //Prints S as number
    type "\n"  //Type New Line
    add 1      //Higher S by 1
seal           //This Loop prints the digits 0..9

func getFive  //Declares Function getFive
    exit 5    //makes the function return 5
seal
```

***

### FUNC:
Parameters: `Function Name`  
Uses seal block  
Description:  
Declares a function, that executes the code between it and its corresponding seal instruction. The function can later be called by the given name in the parameter. After defining the function, the interpreter continues after the seal instruction and does not execute the function.  
Example:  
```
func checkForEven  //Declares Function
    stow A       //Temporarily stores S so it can be taken again later
    copy A      
    rem 2        //Get the remainder between S and 2
    case S == 0  //Remainder is 0 -> even number
        type "S is an even number"
    seal
    else        //Otherwise odd number
        type "S is an odd number"
    seal
    dump        //Clear S from the remainder
    take A      //And take its initial value again
seal            //End of Function
```

***

### EXEC:
Parameters: `Function Name`  
Description:  
Executes the function with the given function name, if it is present. If the function returns a value, it is stored to the SLOT (S).  
Example:  
```
copy 5
exec checkForEven   //Prints: S is an odd number

func getEleven
    dump     //This is also allowed to make sure S is empty when returning a value
    exit 11
seal

exec getEleven
```

***

### TURN:
Parameters: `None or Number`  
Description:  
Changes the identifiers of the 3 stacks. When turning by 1, stack A now holds the values of B, stack B holds C, stack C holds A. Turning by 2 means, stack A now holds the values of C, stack B holds A, stack C hold B. Turning is additive, which means turning 2 times by 1 would be a total turn of 2. When no parameter is given, it turns by 1.  
Example:  
```
copy 100
stow A
turn 1
take C
give    //Prints 100
```

***

### WAIT:
Parameters: `Number`  
Description:  
Pauses the interpreter for the specified numbers in the parameter in milliseconds. Only positive parameters and zero are allowed.  
Example:  
```
copy 0
loop ever      //Endless loop
    wait 1000  //Wait one second
    add 1      //Add 1 to S
    give       //Print S to console
    type "\n"  //Type New Line
seal      //This loop counts the seconds passed since the start of the programm.
```

***

### OPEN:
Parameters: `String`  
Description:  
Executes the script file of the given path, if it is present. If the file returns a value, it is stored to the SLOT (S). The string that provides the path does not support escape sequences. The path may be absolute or relative to the current file. The same stacks and the same SLOT (S) are shared among all script files.  
Example:  
```
open "script1.hanoi"   //Open file from relative path
open "C:/Users/Max/Desktop/script2.hanoi"    //Open file from absolute path
```

***

### TEXT:
Parameters: `String`  
Uses seal block  
Description:  
Opens the text file of the given path, if it is present. Then, for each character inside the text file, it is stored to the SLOT (S) and then the instructions between the file and the seal instructions are executed, which means at the beginning of the file loop the slot always has to be empty. It can be exited with an exit instruction and single characters can be skipped with a skip instruction. The string that provides the path does not support escape sequences. The path may be absolute or relative to the current file.  
Example:  
```
dump    //Clearing slot, so the next character can be taken
text "example.txt"    //Open the file
    type    //Type The character to the console
    dump    //Clearing slot, so the next character can be taken and it does not throw an exception
seal    //This file loop prints the contents of the file "example.txt" to the console
```

***

### FILE:
Parameters: `String`  
Description:  
Opens the file of the given path, with the default application picked by the operating system if it is present. The path may be absolute or relative to the current file.  
Example:  
```
file "image.jpg"    //Open an image file
file "script.bat"   //Run a batch script
```

## Operators
### ADD:
Parameters: `Number`  
Description:  
Adds the parameter to the value of the SLOT (S) and saves it to the SLOT.  
Example:  
```
copy 5
add 5
give   //Prints 10
add -13
give   //Prints -3
```

***

### SUB:
Parameters: `Number`  
Description:  
Subtracts the parameter from the value of the SLOT (S) and saves it to the SLOT.  
Example:  
```
copy 5
sub 5
give   //Prints 0
sub -13
give   //Prints 13
```

***

### MUL:
Parameters: `Number`  
Description:  
Multiplies the parameter with the value of the SLOT (S) and saves it to the SLOT.  
Example:  
```
copy 5
mul 5
give   //Prints 25
mul -2
give   //Prints -50
```

***

### DIV:
Parameters: `Number`  
Description:  
Divides the parameter from the value of the SLOT (S) and saves it to the SLOT. Fractions are rounded towards zero.  
Example:  
```
copy 10
div 2
give   //Prints 5
div 2
give   //Prints 2
```

***

### REM:
Parameters: `Number`  
Description:  
Divides the parameter from the value of the SLOT (S) and saves the remainder to the SLOT.  
Example:  
```
copy 5
rem 2
give   //Prints 1
```

***

### POW:
Parameters: `Number`  
Description:  
Takes the value of the SLOT (S) to the power of the parameter and saves it to the SLOT. Negative exponents are allowed but fractions are rounded towards zero.  
Example:  
```
copy 4
pow 2
give  //Prints 16
pow 2
give  //Prints 256
```

***

### SHL:
Parameters: `Number`  
Description:  
Shifts the value of the SLOT (S) left by the amount of bits specified in the parameter and saves it to the SLOT. The high order bits that shift out of the range are discarded and do not loop around. Only positive parameters and zero are allowed.  
Example:  
```
copy 4
shl 1
give  //Prints 8
shl 2
give  //Prints 32
```

***

### SHR:
Parameters: `Number`  
Description:  
Shifts the value of the SLOT (S) right by the amount of bits specified in the parameter and saves it to the SLOT. The low order bits that shift out of the range are discarded and do not loop around. Only positive parameters and zero are allowed.  
Example:  
```
copy 32
shr 2
give  //Prints 8
shr 1
give  //Prints 4
```

***

### ABS:
Parameters: `None`  
Description:  
Turns the value of the SLOT (S) into a positive one if it is negative.  
Example:  
```
copy -5
abs
give //Prints 5
```

***

### AND:
Parameters: `Number`  
Description:  
Performs a bitwise and operation on the parameter and the SLOT (S) value and saves it to the SLOT.  
Example:  
```
copy 24
and 12
give  //Prints 8
```

***

### XOR:
Parameters: `Number`  
Description:  
Performs a bitwise xor (exclusive or) operation on the parameter and the SLOT (S) value and saves it to the SLOT.  
Example:  
```
copy 24
xor 12
give  //Prints 20
```

***

### BOR:
Parameters: `Number`  
Description:  
Performs a bitwise or operation on the parameter and the SLOT (S) value and saves it to the SLOT.  
Example:  
```
copy 24
bor 12
give  //Prints 28
```

***

### NOT:
Parameters: `None`  
Description:  
Flips all bits of the SLOT (S) value and saves it to the SLOT.  
Example:  
```
copy 32
not
give  //Prints -33
not
give  //Prints 32
```
