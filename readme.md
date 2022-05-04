# Processor
The processor I am simulating has a very limited instruction set. To compile put "xbuild" in the command line.
## How to use 
The processor uses a long list of booleans to decide how it runs and what it prints out. There are debug booleans which allow classes (eg excution units) to print there outputs. If you want to run your own program (using the instruction set below) change the "RunTests" boolean to false, and write your program in assemblyCode.txt . 
## Instructions
| Instruction | Input | Description
| -------- | -------- | --------------- 
| ADD | store, r1, r2 | Add r1 and r2 store result 
| SUB | store, r1, r2 | Subsitute r1 and r2 store result 
| MUL | store, r1, r2 | Multiply r1 and r2 store result 
| DIV | store, r1, r2 | Divide r1 and r2 store result 
| ADDI | store, r1, value | Add r1 and value store result 
| COMP | store, r1, r2 | Stores -1 if <, 0 if ==, 1 if >
| LDC | store, r1 | Load r1 into store
| LD | storeValueInRegister, index | Load register via index
| BEQ | destination, r1, r2 | if r1 == r2 move PC to destination
| BNE | destination, r1, r2 | if r1 != r2 move PC to destination
| STR | index, storeValueInRegister | Store via index
| JUMP | destination | Move PC to destination
| NOP | Ends Process | Ends process 
## Code Layout 
### Processor 
Contains all the booleans to change the readout the processor puts in the console (e.g. Turning on ReserveStationReadOut means that when the reservation station will print it's log to the console).
This script also starts the processor and checks that the answers are right if it is in test mode.
### Pipes
Pipes fetchs, decodes and sends information to excution unit. It uses pipeData to store the decoded information. It also has a list of commands that have been sent back if the reserve stations are too full, this list takes proprity over commands coming in. Only one pipe can fetch at one time.
### Memory
Has simple functions to put/read data from an array (simulated caches).
### Excution Unit
This conducts all the excution along with changing the data struct from pipeData to command. Keeps track of reservation stations and sends commands to the reorder buffer along with the cycle they were completed. This data is used in the reorder buffer to track dependencies.
### ReOrder Buffer
The reorder buffer commits commands to memory while checking to see if there are dependencies between them. If a dependency is caught then the command is sent back.
### Branch Prediction
The branch predictor is sent any branch commands, giving them an speculative result and pushing them to the RoB. We also tell pipes where we want to start a speculative branch, this means the pipe will decode and excute the speculative branch (these commands are a lower priority then commands that aren't speculative). The branch predictor has an int value that decides waht type of predictor it will be, it has types fixed (take and not take) dynamic (backward and forward) along with one and two state predictors.  
## Test Cases
1. Work out Factorial of x 
2. Work out x in the fibonacci sequence
3. Do x number of simple additions 
4. Bubble sort
5. Binary search
