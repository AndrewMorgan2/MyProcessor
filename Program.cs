using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyProcessor
{
    #region DataStructures
    static class Constants
    {
        //Because it's a byte 
        public const int MAX_INT_FOR_REGISTER = 256;
    }
    struct pipeData
    {
        public int Name;
        public string CommandHistory;
        public string ActiveCommand;
        public int instructionCycles;
    }

    struct cache{
        public byte[] memory;
    }
    struct registerFile{
        public cache[] caches; 
    }
    #endregion

    class Program
    {
        static int NumberOfPipes = 3;
        static int SizeOfCache = 10;
        static int NumberOfCache = 3;
        #region Counting Vars for benchmarking
        static int waitingCycles = 0;
        static int[] cacheCalls = new int[NumberOfCache];
        static int cacheMisses = 0;
        #endregion
        static void Main(string[] args)
        {
            Console.WriteLine("----------------  Starting   ----------------");
            #region Instatiate pipes and register file
            //Instatiate Pipes
            pipeData[] pipes = new pipeData[NumberOfPipes];
            for(int i = 0; i < NumberOfPipes; i++)
            {
                pipes[i].Name = i;
                pipes[i].ActiveCommand = null;
                pipes[i].CommandHistory = null;
                pipes[i].instructionCycles = 0;
            }
            //Instatiate cache
            registerFile regFile = new registerFile{
                caches = new cache[NumberOfCache]
            };

            for(int i = 0; i < NumberOfCache; i++){
                cache cacheToBeAddedToRegister = new cache{ 
                memory = new byte[SizeOfCache]
                };
                regFile.caches[i] = cacheToBeAddedToRegister;
            }
            #endregion
            PutValueInRegister(1, 12, ref regFile);
            Console.WriteLine($"Value in register {(int)regFile.caches[0].memory[1]}");

            //Read in commands into a useable arrray
            string[] instructionList = System.IO.File.ReadAllLines(@"./instructionSet.txt");
            System.Console.WriteLine($"Now lets run our processor with {instructionList.Length} commands");
            int instructionNumber = 0;
            //Do we still have instruction to excute
            while(instructionList.Length > instructionNumber){
                //Assign jobs to pipes
                PipeAssignment(ref pipes, instructionList, ref instructionNumber);
                PipeExcute(ref pipes);
            }
            Console.WriteLine("All instructions have been fetched!");
            
            //Excute the instructions, that are still in pipes
            int pipesClear = 0;
            while(pipesClear < NumberOfPipes){
                pipesClear = 0;
                //Check if the pipes are clear
                for(int i = 0; NumberOfPipes > i; i++){
                    //if the pipe is done or not used
                    if(pipes[i].ActiveCommand == "Waiting" || pipes[i].ActiveCommand == null){
                        pipesClear++;
                    }
                }
                //Assign jobs to pipes
                PipeFinish(ref pipes);
                //Excute 
                PipeExcute(ref pipes);
            }
            #region Printing Processor History
            Console.WriteLine("----------------  Pipes history    ----------------");
            int actionsTaken = 0;
            for(int i = 0; NumberOfPipes > i; i++){
                if(pipes[i].CommandHistory == null) System.Console.WriteLine($" Pipe {pipes[i].Name} wasn't used");
                else{
                    System.Console.Write($" --- Pipe {pipes[i].Name}'s History");
                    System.Console.WriteLine(pipes[i].CommandHistory);
                    actionsTaken = actionsTaken + pipes[i].instructionCycles;
                }
            }
            //End of program
            Console.WriteLine($"Actions taken {actionsTaken}");
            Console.WriteLine($"Waits taken {waitingCycles}");
            Console.WriteLine("----------------   Memory stats    ----------------");
            for(int i = 0; NumberOfCache > i; i++){
                Console.WriteLine($"Cache calls for cache {i} = {cacheCalls[i]}");
            }
            Console.WriteLine($"Number of cache misses {cacheMisses}");
            #endregion
        }

        static void processCommands(string command, ref pipeData pipe){
            //if the pipe is sleeping then we just leave
            if(command == "Waiting"){
                //Wait
            }
            else if(command.Substring(0,5) == "Fetch"){
                //We fetch 
            }
            else if(command == "Decode"){
                //We decode 
            }
            else if(command == "Excution"){
                //We excute
                string opCode = getNextPartFromText(command);
                excutionUnitManager(opCode, command, ref pipe);
            }
            return;
        }
        #region Memory Functions
        static void PutValueInRegister(int registerIndex, int value, ref registerFile regFile){
            //Find which cache do we need to hit
            int cacheNumber = 0;
            if(registerIndex > SizeOfCache){
                registerIndex = registerIndex - SizeOfCache;
                cacheNumber++;
            }
            //Check if we have that register
            if(cacheNumber > NumberOfCache){
                Console.WriteLine("Error ----- Index sent that doesn't exist in the register file");
                //Cache Miss
                cacheMisses++;
            } 

            //Debug for what we are doing in the memory
            if(regFile.caches[cacheNumber].memory[registerIndex] == 0){
                Console.WriteLine($"We aren't writing over anything in cache{cacheNumber} at {registerIndex}");
            } else Console.WriteLine($"We are writing over in cache{cacheNumber} at {registerIndex} it had value {regFile.caches[cacheNumber].memory[registerIndex]}");
            
            //Update benchmark parameters
            cacheCalls[cacheNumber]++;

            //Actually set values in the memory 
            regFile.caches[cacheNumber].memory[registerIndex] = (byte)value;
            Console.WriteLine($"{regFile.caches[cacheNumber].memory[registerIndex]}");
        }
        #endregion
        #region Excution Units
        /*Excution Units*/
        static void excutionUnitManager(string opCode, string command, ref pipeData pipe){
            //Update pipeline
            PipeReplaceCommand(pipe.ActiveCommand, "Excute", ref pipe);

            //Here's where we decide what to actually do
            arthmetricOp(opCode, command);

            //Update pipeline
            PipeReplaceCommand(pipe.ActiveCommand, null, ref pipe);
        }
        static void arthmetricOp(string opcode, string command){
            command = command.Remove(0,4);
            //We need the next part of the instruction
            //r1 can be an int or a direction to a register
            string r1 = getNextPartFromText(command);
            //r2 can be an int or a direction to a register
            command = command.Remove(0, r1.Length);
            string r2 = getNextPartFromText(command);
            if(opcode == "ADD"){
                addEU(command, r1, r2);
            }
            if(opcode == "SUB"){
                subEU(command, r1, r2);
            }
        }
        static void addEU(string command, string r1, string r2){
            //ADD r1 r2 
            int result = Int32.Parse(r1) + Int32.Parse(r2);
            System.Console.WriteLine($"Adding {r1} to {r2}: Result {result}");
        }
        static void subEU(string command, string r1, string r2){
            //SUB r1 r2 
            int result = Int32.Parse(r1) - Int32.Parse(r2);
            System.Console.WriteLine($"Subtracting {r1} to {r2}: Result {result}");
        }
        #endregion

        #region Pipe Functions
        static void PipeReplaceCommand(string oldCommand, string newCommand, ref pipeData pipe){
            //aConsole.WriteLine($"Pipe:{pipe.Name} has gone from {oldCommand} to {newCommand}");
            if(oldCommand != null){
                //Add old Command to history 
                pipe.CommandHistory = pipe.CommandHistory + " | " + oldCommand;
            }
            //Replace new active command
            pipe.ActiveCommand = newCommand;
            if(newCommand != null){
                pipe.instructionCycles++;
            }
        }
        static void PipeAssignment(ref pipeData[] pipes, string[] instructionList, ref int instructionNumber){
            //Assign instruction to pipe 
            for(int i = 0; NumberOfPipes > i; i++){
                if(pipes[i].ActiveCommand == null || pipes[i].ActiveCommand == "Waiting"){
                    //Console.WriteLine($"Pipe {pipes[i].Name} has been given: {instructionList[instructionNumber]}");
                    PipeReplaceCommand(pipes[i].ActiveCommand, String.Format($"Fetch {instructionList[instructionNumber]}"), ref pipes[i]);
                    instructionNumber++;

                    //What are all the other pipes up to 
                    for(int b = 0; NumberOfPipes > b; b++){
                        //we know what pipes[i] is doing
                        if(pipes[b].Name != pipes[i].Name){
                            //Lets update the instructions of the pipes
                            if(pipes[b].ActiveCommand == null){
                                //Console.WriteLine($"pipe {pipes[b].Name} is going to wait");
                                PipeReplaceCommand(pipes[b].ActiveCommand, "Waiting", ref pipes[b]);
                                //Console.WriteLine($"Pipe {pipes[b].Name} is waiting");
                                //Add one to total waiting cycles
                                waitingCycles++;
                            } 
                            else if(pipes[b].ActiveCommand.Substring(0,5) == "Fetch"){
                                PipeReplaceCommand(pipes[b].ActiveCommand, "Decode", ref pipes[b]);
                            }
                            else if(pipes[b].ActiveCommand == "Decode"){
                                PipeReplaceCommand(pipes[b].ActiveCommand, "Excute", ref pipes[b]);
                            }
                            else if(pipes[b].ActiveCommand == "Excute"){
                                //We shoudn't see excute 
                                //It should be updated in excution unit

                            }
                            else if(pipes[b].ActiveCommand == "Waiting"){
                                //Do nothing
                            }
                            else {
                                Console.WriteLine($"Pipe {pipes[b].Name} has a unrecognised active command");
                            }
                        }
                    }
                    /*BREAK AHEAD NOT GOOD PRACTICE SOLVE IN FUTURE*/
                    //We don't want all empty pipes to be assigned the same task
                    break;  
                }
            }
        }
        //Finish the pipe line 
        static void PipeFinish(ref pipeData[] pipes){
            //What are all the other pipes up to 
            for(int b = 0; NumberOfPipes > b; b++){
                //Lets update the instructions of the pipes
                if(pipes[b].ActiveCommand == null){
                    //Console.WriteLine($"pipe {pipes[b].Name} is going to wait");
                    PipeReplaceCommand(pipes[b].ActiveCommand, "Waiting", ref pipes[b]);
                    //Console.WriteLine($"Pipe {pipes[b].Name} is waiting");
                    //Add one to total waiting cycles
                    waitingCycles++;
                } 
                else if(pipes[b].ActiveCommand.Substring(0,5) == "Fetch"){
                    PipeReplaceCommand(pipes[b].ActiveCommand, "Decode", ref pipes[b]);
                }
                else if(pipes[b].ActiveCommand == "Decode"){
                    PipeReplaceCommand(pipes[b].ActiveCommand, "Excute", ref pipes[b]);
                }
                else if(pipes[b].ActiveCommand == "Excute"){
                    //We shoudn't see excute 
                    //It should be updated in excution unit
                    PipeReplaceCommand(pipes[b].ActiveCommand, "Waiting", ref pipes[b]);
                }
                else if(pipes[b].ActiveCommand == "Waiting"){
                    //Do nothing
                }
                else {
                    Console.WriteLine($"Pipe {pipes[b].Name} has a unrecognised active command");
                }
            }
        }
        static void PipeExcute(ref pipeData[] pipes){
            //Excute pipe instructions
             for(int i = 0; NumberOfPipes > i; i++){
                //Console.WriteLine("Pipe Run");
                //We dont process null commands 
                if(pipes[i].ActiveCommand != null){
                    //This is aync
                    processCommands(pipes[i].ActiveCommand, ref pipes[i]);
                }
            }
        }
        #endregion

        #region Parsing Text
        //Gets next part of the instruction eg (opcode/ register/ int/)
        static string getNextPartFromText(string command){
            string opcode = "";
            //check if the first x chars are empty spaces
            int numberOfSpaces = 0;
            foreach(char character in command){
                if(character == ' '){
                    numberOfSpaces++;
                } else break;
            }
            //Now we can actually get the next part
            command = command.Remove(0, numberOfSpaces);
            foreach(char character in command){
                if(character != ' '){
                    opcode = opcode + character;
                }
                else return opcode;
            }
            return opcode;
        }
        #endregion
    }
}
