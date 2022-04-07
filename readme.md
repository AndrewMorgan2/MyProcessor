# Processor
The processor I am simulating has a very limited instruction set.
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
| LD | store, r1, offset | Load r1 with offset store 
| BEQ | destination, r1, r2 | if r1 == r2 move PC to destination
| BNE | destination, r1 | if r1 != r2 move PC to destination
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
## Future Work
branch predictors
all types of branch prediction
look for tree to make two state branch prediction bad and write about it
## Test Cases
1. Work out Factorial of x 
2. Work out x in the fibonacci sequence
