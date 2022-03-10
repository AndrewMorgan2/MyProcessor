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
        public string Opcode;
        public string Destination;
        public int[] valueRegisters;
        public int instructionCycles;
    }

    struct cache{
        public int[] memory;
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
        //This is the number of cycles before we force quit (used to detect infinite loops in a very simple way)
        static int CycleLimit = 100;
        #region Counting Vars for benchmarking and debug bools
        static bool PipeDebug = true;
        static bool MemoryDebug = false;
        static bool ExcutionUnitDebug = false;
        static bool WriteBackDebug = true;
        static bool MemoryReadOut = true;
        static bool InfiniteLoopDetection = true;
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
            //Counting cycles for infiniteLoopDetection
            int cycles = 0;
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
                cycles ++;
                if(InfiniteLoopDetection == true){
                    if(cycles > CycleLimit) {
                        Console.WriteLine("CYCLE LIMIT REACHED, INFINITE CYLCE DETECTED");
                        break;
                    }
                }
            }
            #endregion

            #region Printing Processor History
            if(PipeDebug == true){
                Console.WriteLine("----------------  Pipes history    ----------------");
                int actionsTaken = 0;
                for(int i = 0; NumberOfPipes > i; i++){
                    if(Pipe.pipes[i].CommandHistory == null) System.Console.WriteLine($" --- Pipe {Pipe.pipes[i].Name} wasn't used");
                    else{
                        System.Console.Write($" --- Pipe {Pipe.pipes[i].Name}'s History");
                        System.Console.WriteLine(Pipe.pipes[i].CommandHistory);
                        actionsTaken = actionsTaken + Pipe.pipes[i].instructionCycles;
                    }
                }
                Console.WriteLine($"Actions taken {actionsTaken}");
                Console.WriteLine($"Waits taken {waitingCycles}");
            }
            if(MemoryDebug == true){
                Console.WriteLine("----------------   Memory stats    ----------------");
                for(int i = 0; NumberOfCache > i; i++){
                    Console.WriteLine($"Cache calls for cache {i} = {cacheCalls[i]}");
                }
                Console.WriteLine($"Number of cache misses {cacheMisses}");
            }
            if(MemoryReadOut == true){
                Console.WriteLine("----------------   Memory readOut    ----------------");
                string readOut = "";
                for(int i = 0; i < NumberOfCache; i++){
                    readOut = readOut + $"\n Cache {i} contained: ";
                    for(int a = 0; a < SizeOfCache; a++){
                        readOut = readOut + $"r{a + (i*SizeOfCache)} " + Memory.Regfile.caches[i].memory[a] + " | ";
                    }
                }
                Console.WriteLine(readOut);
            }
            #endregion
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
                    memory = new int[SizeOfCache]
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
                //Update benchmark parameters
                cacheCalls[cacheNumber]++;

                //Actually set values in the memory 
                Regfile.caches[cacheNumber].memory[registerIndex] = value;
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
                string debug = String.Format($"Opcode recieved: {Pipe.pipes[pipeName].Opcode}, with Destination: {Pipe.pipes[pipeName].Destination}, regVal1:{Pipe.pipes[pipeName].valueRegisters[0]} and regVal2:{Pipe.pipes[pipeName].valueRegisters[1]}");
                DebugPrint(debug);

                //Here's where we decide what to actually do
                //REGISTER COMMANDS
                if(Pipe.pipes[pipeName].Opcode == "LD"){
                    //Load Register via offset
                    //Sorted to ldc at decode so we shouldn't ever run this 
                    Console.WriteLine("ERROR ----- We have command LD where we should have LDC, maybe decode failed?");
                }
                if(Pipe.pipes[pipeName].Opcode == "LDC"){
                    //Load Register directly
                    loadDirectly(Pipe.pipes[pipeName].Destination, Pipe.pipes[pipeName].valueRegisters[0]);
                }

                //BRANCH COMMANDS
                if(Pipe.pipes[pipeName].Opcode == "BEQ"){
                    BranchEqual(Pipe.pipes[pipeName].valueRegisters[0], Pipe.pipes[pipeName].valueRegisters[1]);
                }
                if(Pipe.pipes[pipeName].Opcode == "BNE"){
                    BranchNotEqual(Pipe.pipes[pipeName].valueRegisters[0], Pipe.pipes[pipeName].valueRegisters[1]);
                }

                //ARTHEMETRIC
                if(Pipe.pipes[pipeName].Opcode == "ADDI"){
                    addiEU(Pipe.pipes[pipeName].Destination, Pipe.pipes[pipeName].valueRegisters[0], Pipe.pipes[pipeName].valueRegisters[1]);
                }
                if(Pipe.pipes[pipeName].Opcode == "ADD"){
                    addEU(Pipe.pipes[pipeName].Destination, Pipe.pipes[pipeName].valueRegisters[0], Pipe.pipes[pipeName].valueRegisters[1]);
                }
                if(Pipe.pipes[pipeName].Opcode == "SUB"){
                    subEU(Pipe.pipes[pipeName].Destination, Pipe.pipes[pipeName].valueRegisters[0], Pipe.pipes[pipeName].valueRegisters[1]);
                }
                if(Pipe.pipes[pipeName].Opcode == "COMP"){
                    compare(Pipe.pipes[pipeName].Destination, Pipe.pipes[pipeName].valueRegisters[0], Pipe.pipes[pipeName].valueRegisters[1]);
                }
            }
            static void addEU(string r1, int r2, int r3){
                //ADD r1 = r2 + r3
                int result = r2 + r3;
                //System.Console.WriteLine($"Adding {r2} to {r3}: Result {result}");
                Memory.PutValueInRegister(r1, result);
                //Write Back Debug
                DebugPrintWriteBack($"Add Write Back {result}");
            }
            static void subEU(string r1, int r2, int r3){
                //SUB r1 = r2 - r3 
                int result = r2 - r3;
                //System.Console.WriteLine($"Subtracting {r2} to {r3}: Result {result}");
                Memory.PutValueInRegister(r1, result);
                //Write Back Debug
                DebugPrintWriteBack($"Sub Write Back {result}");
            }
            static void addiEU(string r1, int r2, int r3){
                //ADDI r1 increamented by r2(value)
                int result = r2 + r3;
                //System.Console.WriteLine($"Adding register {r1} to {x}: Result {result}");
                Memory.PutValueInRegister(r1, result);
                //Write Back Debug
                DebugPrintWriteBack($"AddI Write Back {result}");
            }
            static void loadDirectly(string r1, int r2){
                //Load r2's value into r1
                //Console.WriteLine($"Loading value {registesCurrentValue} into {r1}");
                Memory.PutValueInRegister(r1, r2);
                //Write Back Debug
                DebugPrintWriteBack($"Load Write Back: loaded {r2} into {r1}");
            }
            static void compare(string r1, int r2, int r3){
                if(r2 < r3){
                    Memory.PutValueInRegister(r1, -1);
                    //Write Back Debug
                    DebugPrintWriteBack($"Compare Write Back: loaded {-1} into {r1}");
                }
                else if(r2 > r3){
                    Memory.PutValueInRegister(r1, 1); 
                    //Write Back Debug
                    DebugPrintWriteBack($"Compare Write Back: loaded {1} into {r1}");
                }
                else {
                    Memory.PutValueInRegister(r1, 0);                
                    //Write Back Debug
                    DebugPrintWriteBack($"Compare Write Back: loaded {0} into {r1}");
                }
            }
            static void BranchEqual(int newPCPosition, int value){
                if(value == 0) {
                    ProgramCounter = value;
                    DebugPrintWriteBack($"BranchEqual Write Back: changed PC to {value}");
                } else DebugPrintWriteBack($"BranchEqual Write Back: didnt change PC");
            }
            static void BranchNotEqual(int newPCPosition, int value){
                if(value != 0) {
                    ProgramCounter = value;
                    DebugPrintWriteBack($"BranchNotEqual Write Back: changed PC to {value}");
                } else DebugPrintWriteBack($"BranchNotEqual Write Back: didnt change PC");
            }
            static void DebugPrint(string debugPrint){
                if(ExcutionUnitDebug == true){
                    Console.WriteLine(debugPrint);
                }
            }
            static void DebugPrintWriteBack(string debugPrint){
                if(WriteBackDebug == true){
                    Console.WriteLine(debugPrint);
                }
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
                    //We only have two registers per pipe
                    pipes[i].valueRegisters = new int[2];
                }
            }
            static public void PipeReplaceCommand(string oldCommand, string newCommand, int pipeName){
                //aConsole.WriteLine($"Pipe:{pipe.Name} has gone from {oldCommand} to {newCommand}");
                if(oldCommand != null){
                    string commandToBeAdded = "";
                    if(Pipe.pipes[pipeName].ActiveCommand == null || Pipe.pipes[pipeName].ActiveCommand == "Waiting"){
                        commandToBeAdded = "WT";
                    } 
                    else if(Pipe.pipes[pipeName].ActiveCommand.Substring(0,5) == "Fetch"){
                        commandToBeAdded = "IF";
                    }
                    else if(Pipe.pipes[pipeName].ActiveCommand == "Decode"){
                        commandToBeAdded = "DE";
                    }
                    else if(Pipe.pipes[pipeName].ActiveCommand == "Excute"){
                        commandToBeAdded = "EX";
                    }
                    else {
                        Console.WriteLine($"Pipe {Pipe.pipes[pipeName].Name} has a unrecognised active command");
                    }
                    //Add old Command to history 
                    Pipe.pipes[pipeName].CommandHistory = Pipe.pipes[pipeName].CommandHistory + " | " + commandToBeAdded;
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
                            //Console.WriteLine($"Pipe {pipes[i].Name} has been given: {instructionList[ProgramCounter]}");
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
                Excute();
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
            static void Excute(){
                //Excute pipe instructions
                for(int i = 0; NumberOfPipes > i; i++){
                    //Console.WriteLine($"{pipes[i].ActiveCommand}");
                    processCommands(pipes[i].ActiveCommand, pipes[i].Name);
                }
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
                Decode(pipeName);
            }
            else if(command == "Excute"){
                //We excute
                ALU.excutionUnitManager(pipeName);
            }
            return;
            }
            static void Decode(int pipeName){
                //We don't want the fetch so we get rid of that straight away 
                string currentInstruction =  Pipe.pipes[pipeName].CurrentInstruction;
                string opCode = getNextPartFromText(currentInstruction);
                currentInstruction = currentInstruction.Remove(0,opCode.Length + 1);
                string destination = getNextPartFromText(currentInstruction);
                currentInstruction = currentInstruction.Remove(0,destination.Length + 1);

                string r2;
                //Change LD to LDC
                if(opCode == "LD"){
                    r2 = getNextPartFromText(currentInstruction);
                    currentInstruction = currentInstruction.Remove(0,r2.Length + 1);
                    string r3 = currentInstruction;
                    //Get value from register here
                    //We leave r2 as a register
                    int valueLoaded = 0;
                    if(r3.Contains('r') == true) {
                        valueLoaded = Memory.GetValueFromRegister(r3);
                    } else valueLoaded =  Int32.Parse(r3);
                    opCode = "LDC";
                    Pipe.pipes[pipeName].valueRegisters[0] = valueLoaded;
                } 
                //Decode Happens here
                else {
                    r2 = getNextPartFromText(currentInstruction);
                    //Get value from register here (if possible)
                    if(r2.Contains('r') == true) {
                        Pipe.pipes[pipeName].valueRegisters[0] = Memory.GetValueFromRegister(r2);
                    } else Pipe.pipes[pipeName].valueRegisters[0] =  Int32.Parse(r2);
                    //Checks if there is more to decode
                    if(currentInstruction.Length > r2.Length + 1){
                        currentInstruction = currentInstruction.Remove(0,r2.Length + 1);
                        string r3 = currentInstruction;
                        //Get value from register here (if possible)
                        if(r3.Contains('r') == true) {
                            Pipe.pipes[pipeName].valueRegisters[1] = Memory.GetValueFromRegister(r3);
                        } else Pipe.pipes[pipeName].valueRegisters[1] =  Int32.Parse(r3);
                    }
                }
                //Give opCode and destination
                Pipe.pipes[pipeName].Opcode = opCode;
                Pipe.pipes[pipeName].Destination = destination;
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
