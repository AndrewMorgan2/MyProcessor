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
        public string CurrentInstruction;
        public string[] DecodedInstruction;
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

        static int ProgramCounter = 0;
        #region Counting Vars for benchmarking
        static int waitingCycles = 0;
        static int[] cacheCalls = new int[NumberOfCache];
        static int cacheMisses = 0;
        #endregion
        static void Main(string[] args)
        {
            Console.WriteLine("----------------  Starting   ----------------");
            #region Instatiate pipes and register file
            //Instatiate Pipes and Memory
            Pipe.makePipes();
            Memory.makeMemory();
            #endregion
            
            #region Run Processor
            //Read in commands into a useable arrray
            string[] instructionList = System.IO.File.ReadAllLines(@"./instructionSet.txt");
            System.Console.WriteLine($"Now lets run our processor with {instructionList.Length} commands");
            //Do we still have instruction to excute
            while(instructionList.Length > ProgramCounter){
                //Assign jobs to pipes
                Pipe.PipeAssignment(instructionList, ref ProgramCounter);
            }
            Console.WriteLine("All instructions have been fetched!");
            
            //Excute the instructions, that are still in pipes
            int pipesClear = 0;
            while(pipesClear < NumberOfPipes){
                //Assign jobs to pipes
                Pipe.PipeAssignment(instructionList, ref ProgramCounter);
                pipesClear = 0;
                //Check if the pipes are clear
                for(int i = 0; NumberOfPipes > i; i++){
                    //if the pipe is done or not used
                    if(Pipe.pipes[i].ActiveCommand == "Waiting" || Pipe.pipes[i].ActiveCommand == null){
                        pipesClear++;
                    }
                }
            }
            #endregion

            #region Printing Processor History
            Console.WriteLine("----------------  Pipes history    ----------------");
            int actionsTaken = 0;
            for(int i = 0; NumberOfPipes > i; i++){
                if(Pipe.pipes[i].CommandHistory == null) System.Console.WriteLine($" --- Pipe {Pipe.pipes[i].Name} wasn't used");
                else{
                    System.Console.Write($" --- Pipe {Pipe.pipes[i].Name}'s History");
                    System.Console.Write(Pipe.pipes[i].CommandHistory);
                    System.Console.WriteLine(" | " + Pipe.pipes[i].ActiveCommand);
                    actionsTaken = actionsTaken + Pipe.pipes[i].instructionCycles;
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

        static void processCommands(string command, int pipeName){
            //Console.WriteLine($"Command: {command} in pipe: {pipeName}");
            //if the pipe is sleeping then we just leave
            if(command == "Waiting"){
                //Wait
                //Add one to total waiting cycles
                waitingCycles++;
            }
            else if(command.Substring(0,5) == "Fetch"){
                //We fetch 
                Pipe.pipes[pipeName].CurrentInstruction =  Pipe.pipes[pipeName].ActiveCommand.Remove(0,5);
            }
            else if(command == "Decode"){
                //We decode 
                //We don't want the fetch so we get rid of that straight away 
                string currentInstruction =  Pipe.pipes[pipeName].CurrentInstruction;
                string opCode = getNextPartFromText(currentInstruction);
                currentInstruction = currentInstruction.Remove(0,opCode.Length + 1);
                string r1 = getNextPartFromText(currentInstruction);
                currentInstruction = currentInstruction.Remove(0,r1.Length + 1);
                //MAGIC NUMBER
                string r2;
                if(currentInstruction.Length > 3){
                    r2 = getNextPartFromText(currentInstruction);
                    currentInstruction = currentInstruction.Remove(0,r2.Length + 1);
                    string r3 = currentInstruction;
                    string[] decodedInstruction = {opCode, r1, r2, r3};
                    Pipe.pipes[pipeName].DecodedInstruction = decodedInstruction;
                } else{
                    r2 = currentInstruction;
                    string[] decodedInstruction = {opCode, r1, r2};
                    Pipe.pipes[pipeName].DecodedInstruction = decodedInstruction;
                }
            }
            else if(command == "Excute"){
                //We excute
                ALU.excutionUnitManager(pipeName);
            }
            return;
        }
        
        #region Memory Functions
        public class Memory{
            static public registerFile Regfile = new registerFile();
            static public void makeMemory(){
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
                Regfile = regFile;
            }
            static public void PutValueInRegister(string registerIndexString, int value){
                if(registerIndexString.Contains('r') == false) Console.WriteLine("Register passed without register flag!");
                //Remove the r
                int registerIndex = RemoveR(registerIndexString);
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

                /*
                //Debug for what we are doing in the memory
                if(Regfile.caches[cacheNumber].memory[registerIndex] == 0){
                    Console.WriteLine($"We aren't writing over anything in cache{cacheNumber} at {registerIndex}");
                } else Console.WriteLine($"We are writing over in cache{cacheNumber} at {registerIndex} it had value {Regfile.caches[cacheNumber].memory[registerIndex]}");
                */
                //Update benchmark parameters
                cacheCalls[cacheNumber]++;

                //Actually set values in the memory 
                Regfile.caches[cacheNumber].memory[registerIndex] = (byte)value;
                //Console.WriteLine($"{Regfile.caches[cacheNumber].memory[registerIndex]} is the value in in cache:{cacheNumber}, register:{registerIndex}");
            }
            static public int GetValueFromRegister(string registerIndexString){
                if(registerIndexString.Contains('r') == false) {
                    Console.WriteLine("Register passed without register flag! --- Cache miss");
                    cacheMisses++;
                    return 0;
                }
                //Remove the r
                int registerIndex = RemoveR(registerIndexString);
                //Find which cache do we need to hit
                int cacheNumber = 0;
                if(registerIndex > SizeOfCache){
                    registerIndex = registerIndex - SizeOfCache;
                    cacheNumber++;
                }
                return Regfile.caches[cacheNumber].memory[registerIndex];
            }
            static public int GetValueFromRegisterWithOffset(string registerIndexString, string offset){
                if(registerIndexString.Contains('r') == false) {
                    Console.WriteLine("Register passed without register flag! --- Cache miss");
                    cacheMisses++;
                    return 0;
                }
                //Remove the r
                int registerIndex = RemoveR(registerIndexString);
                //Add the offset
                registerIndex = registerIndex + Int32.Parse(offset);
                //Find which cache do we need to hit
                int cacheNumber = 0;
                if(registerIndex > SizeOfCache){
                    registerIndex = registerIndex - SizeOfCache;
                    cacheNumber++;
                }
                return Regfile.caches[cacheNumber].memory[registerIndex];
            }
            static int RemoveR(string r1){
                //Remove the r
                int rPos = 0;
                for(int i = 0; i < r1.Length; i++){
                    rPos++;
                    if(r1[i] == 'r') break;
                }
                return Int32.Parse(r1.Remove(0,rPos));
            }
        }
        #endregion
        
        #region Excution Units
        static class ALU{
            /*Excution Units*/
            static public void excutionUnitManager(int pipeName){
                if(Pipe.pipes[pipeName].DecodedInstruction.Length == 3){
                    Console.WriteLine($"Opcode recieved: {Pipe.pipes[pipeName].DecodedInstruction[0]}, with r1: {Pipe.pipes[pipeName].DecodedInstruction[1]} and r2:{Pipe.pipes[pipeName].DecodedInstruction[2]}");
                } 
                else if(Pipe.pipes[pipeName].DecodedInstruction.Length == 4){
                    Console.WriteLine($"Opcode recieved: {Pipe.pipes[pipeName].DecodedInstruction[0]}, with r1: {Pipe.pipes[pipeName].DecodedInstruction[1]}, r2:{Pipe.pipes[pipeName].DecodedInstruction[2]} and r3:{Pipe.pipes[pipeName].DecodedInstruction[3]}");
                }else Console.WriteLine("Unsupported decoded instruction");

                //Here's where we decide what to actually do
                //REGISTER COMMANDS
                if(Pipe.pipes[pipeName].DecodedInstruction[0] == "LD"){
                    //Load Register via offset
                    loadOffset(Pipe.pipes[pipeName].DecodedInstruction[1], Pipe.pipes[pipeName].DecodedInstruction[2], Pipe.pipes[pipeName].DecodedInstruction[3]);
                }
                if(Pipe.pipes[pipeName].DecodedInstruction[0] == "LDC"){
                    //Load Register directly
                    loadDirectly(Pipe.pipes[pipeName].DecodedInstruction[1], Pipe.pipes[pipeName].DecodedInstruction[2]);
                }
                //BRANCH COMMANDS
                
                //ARTHEMETRIC
                if(Pipe.pipes[pipeName].DecodedInstruction[0] == "ADDI"){
                    addiEU(Pipe.pipes[pipeName].DecodedInstruction[1], Pipe.pipes[pipeName].DecodedInstruction[2], Pipe.pipes[pipeName].DecodedInstruction[3]);
                }
                if(Pipe.pipes[pipeName].DecodedInstruction[0] == "ADD"){
                    addEU(Pipe.pipes[pipeName].DecodedInstruction[1], Pipe.pipes[pipeName].DecodedInstruction[2], Pipe.pipes[pipeName].DecodedInstruction[3]);
                }
                if(Pipe.pipes[pipeName].DecodedInstruction[0] == "SUB"){
                    subEU(Pipe.pipes[pipeName].DecodedInstruction[1], Pipe.pipes[pipeName].DecodedInstruction[2], Pipe.pipes[pipeName].DecodedInstruction[3]);
                }
            }
            static void addEU(string r1, string r2, string r3){
                //ADD r1 = r2 + r3
                //Getting value from r2 and r3 
                int registesCurrentValueR2 = Memory.GetValueFromRegister(r2);
                int registesCurrentValueR3 = Memory.GetValueFromRegister(r3);
                int result = registesCurrentValueR2 + registesCurrentValueR3;
                //System.Console.WriteLine($"Adding {r2} to {r3}: Result {result}");
                Memory.PutValueInRegister(r1, result);
            }
            static void subEU(string r1, string r2, string r3){
                //SUB r1 = r2 - r3 
                 //Getting value from r2 and r3 
                int registesCurrentValueR2 = Memory.GetValueFromRegister(r2);
                int registesCurrentValueR3 = Memory.GetValueFromRegister(r3);
                int result = registesCurrentValueR2 - registesCurrentValueR3;
                //System.Console.WriteLine($"Subtracting {r2} to {r3}: Result {result}");
                Memory.PutValueInRegister(r1, result);
            }
            static void addiEU(string r1, string r2, string x){
                //ADDI r1 increamented by r2(value)
                //Get Value of r1
                int registesCurrentValue = Memory.GetValueFromRegister(r2);
                int result = registesCurrentValue + Int32.Parse(x);
                //System.Console.WriteLine($"Adding register {r1} to {x}: Result {result}");
                Memory.PutValueInRegister(r1, result);
            }
            static void loadDirectly(string r1, string r2){
                //Load r2's value into r1
                int registesCurrentValue = Memory.GetValueFromRegister(r2);
                //Console.WriteLine($"Loading value {registesCurrentValue} into {r1}");
                Memory.PutValueInRegister(r1, registesCurrentValue);
            }
            static void loadOffset(string r1, string r2, string x){
                //Load r2 + x 's value into r1
                int registesCurrentValue = Memory.GetValueFromRegisterWithOffset(r2, x);
                //Console.WriteLine($"Loading value {registesCurrentValue} into {r1}");
                Memory.PutValueInRegister(r1, registesCurrentValue);
            }
        }
        #endregion

        #region Pipe Functions
        public class Pipe{
            static public pipeData[] pipes = new pipeData[NumberOfPipes];
            static public void makePipes(){
                for(int i = 0; i < NumberOfPipes; i++)
                {
                    pipes[i].Name = i;
                    pipes[i].ActiveCommand = null;
                    pipes[i].CommandHistory = null;
                    pipes[i].instructionCycles = 0;
                }
            }
            static public void PipeReplaceCommand(string oldCommand, string newCommand, int pipeName){
                //aConsole.WriteLine($"Pipe:{pipe.Name} has gone from {oldCommand} to {newCommand}");
                if(oldCommand != null){
                    //Add old Command to history 
                    Pipe.pipes[pipeName].CommandHistory = Pipe.pipes[pipeName].CommandHistory + " | " + oldCommand;
                }
                //Replace new active command
                Pipe.pipes[pipeName].ActiveCommand = newCommand;
                if(newCommand != null){
                    Pipe.pipes[pipeName].instructionCycles++;
                }
            }
            static public void PipeAssignment(string[] instructionList, ref int ProgramCounter){
                //Assign instruction to pipe 
                for(int i = 0; NumberOfPipes > i; i++){
                    if(ProgramCounter < instructionList.Length){
                        if(pipes[i].ActiveCommand == null || pipes[i].ActiveCommand == "Waiting"){
                            Console.WriteLine($"Pipe {pipes[i].Name} has been given: {instructionList[ProgramCounter]}");
                            PipeReplaceCommand(pipes[i].ActiveCommand, String.Format($"Fetch {instructionList[ProgramCounter]}"), pipes[i].Name);
                            ProgramCounter++;

                            //What are all the other pipes up to 
                            for(int b = 0; NumberOfPipes > b; b++){
                                //we know what pipes[i] is doing
                                //We've also already updated all pipes that are smaller than the current i 
                                if(pipes[b].Name != pipes[i].Name && i < b){
                                    //Lets update the instructions of the pipes
                                    UpdatePipe(Pipe.pipes[b].Name);
                                }
                            }
                            /*BREAK AHEAD NOT GOOD PRACTICE SOLVE IN FUTURE*/
                            //We don't want all empty pipes to be assigned the same task
                            break;                   
                        }
                        //If we have full pipes then we just want them to update
                        else UpdatePipe(Pipe.pipes[i].Name);
                    } else {
                        UpdatePipe(Pipe.pipes[i].Name);
                    }
                }
                PipeExcute();
            }
            static void UpdatePipe(int pipeName){
                if(Pipe.pipes[pipeName].ActiveCommand == null || Pipe.pipes[pipeName].ActiveCommand == "Waiting"){
                    //Console.WriteLine($"pipe {pipe.Name} is going to wait");
                    PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Waiting", pipeName);
                    //Console.WriteLine($"Pipe {pipes[b].Name} is waiting");
                } 
                else if(Pipe.pipes[pipeName].ActiveCommand.Substring(0,5) == "Fetch"){
                    PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Decode", pipeName);
                }
                else if(Pipe.pipes[pipeName].ActiveCommand == "Decode"){
                    //Console.WriteLine($"{Pipe.pipes[pipeName].CommandHistory}");
                    PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Excute", pipeName);
                }
                else if(Pipe.pipes[pipeName].ActiveCommand == "Excute"){
                    PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Waiting", pipeName);
                }
                else {
                    Console.WriteLine($"Pipe {Pipe.pipes[pipeName].Name} has a unrecognised active command");
                }
            }
            static void PipeExcute(){
                //Excute pipe instructions
                for(int i = 0; NumberOfPipes > i; i++){
                    //Console.WriteLine($"{pipes[i].ActiveCommand}");
                    processCommands(pipes[i].ActiveCommand, pipes[i].Name);
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
