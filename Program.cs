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
        //Used to debug at the end
        public string CommandHistory;
        //How we know what the pipe is doing
        public string ActiveCommand;
        //What we decode
        public string CurrentInstruction;
        //Opcode from decode
        public string Opcode;
        //Where the operationw will end up
        public string Destination;
        public int[] valueRegisters;
        public int instructionCycles;
        //Used to implement division/multiple/load/store taking more than one cycle
        public int numCyclesBusyFor;
    }
    struct cache{
        public int[] memory;
    }
    struct registerFile{
        public cache[] caches; 
    }
    struct excutionUnit{
        public int numberOfCommandsInTheStation;
        public int cyclesBusyFor;
        public excutionUnitType type;
        public command[] resStation;
        public string historyResStation;
    }
    struct command{
        public string opCode;
        public string destination;
        public int value1;
        public int value2;
    }
    enum excutionUnitType{
        ALU, Branch, LoadStore
    }
    #endregion

    class Program
    {
        #region Processor Stats
        static int NumberOfPipes = 3;
        static int SizeOfCache = 10;
        static int NumberOfCache = 3;
        static int ALUUnitNumber = 3;
        static int BranchUnitNumber = 1;
        static int LoadAndStoreUnitNumber = 1;
        static int SizeOfReservationStation = 3;
        static bool ReservationStationsUsed = true;
        static int ProgramCounter = 0;
        //This is the number of cycles before we force quit (used to detect infinite loops in a very simple way)
        static int CycleLimit = 100;
        #endregion
        #region Number of cycles to do certain operations
        //All of these are extra cycles so 2 extra cycles means 3 total
        //ADD ONE TO ALL TO GET TOTAL CYCLES
        static int divisionCycles = 9;
        static int loadAndStoreCycles = 1;
        static int multiplyCycles = 2;
        #endregion
        #region Counting Vars for benchmarking and debug bools
        static bool PipeDebug = true;
        static bool MemoryDebug = false;
        static bool ExcutionUnitDebug = false;
        static bool WriteBackDebug = false;
        static bool MemoryReadOut = false;
        static bool ReserveStationReadOut = true;
        static bool PipeAssignmentDebug = false;
        static bool InfiniteLoopDetection = true;
        static int waitingCycles = 0;
        static int[] cacheCalls = new int[NumberOfCache];
        static int cacheMisses = 0;
        #endregion
        static void Main(string[] args)
        {
            Console.WriteLine("----------------  Starting   ----------------");
            #region Instatiate pipes, register file and reservation stations
            //Instatiate Pipes and Memory
            Pipe.makePipes();
            Memory.makeMemory();
            ExcutionUnits.makeExcutionUnits();
            #endregion
            
            #region Run Processor
            //Read in commands into a useable arrray
            string[] instructionList = System.IO.File.ReadAllLines(@"./instructionSet.txt");
            System.Console.WriteLine($"Now lets run our processor with {instructionList.Length} commands");
            //Do we still have instruction to excute
            while(instructionList.Length > ProgramCounter){
                //Assign jobs to pipes
                Pipe.PipeAssignment(instructionList, ref ProgramCounter);
                if(ReservationStationsUsed == true){
                    //Run all excution units
                    ExcutionUnits.ProcessReserveStations();
                }
            }
            Console.WriteLine("All instructions have been fetched!");
            
            //Excute the instructions, that are still in pipes
            int pipesClear = 0;
            //Counting cycles for infiniteLoopDetection
            int cycles = 0;
            while(pipesClear < NumberOfPipes){
                //Assign jobs to pipes
                Pipe.PipeAssignment(instructionList, ref ProgramCounter);
                if(ReservationStationsUsed == true){
                    //Run all excution units
                    ExcutionUnits.ProcessReserveStations();
                }
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
            if(ReserveStationReadOut == true && ReservationStationsUsed == true){
                Console.WriteLine("----------------   RS readOut    ----------------");
                for(int i = 0; i < ALUUnitNumber; i++){
                    string debug = $"- Reserve station ALU[{i}] is {ExcutionUnits.ALUunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
                for(int i = 0; i < BranchUnitNumber; i++){
                    string debug = $"- Reserve station Branch[{i}] is {ExcutionUnits.Branchunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
                for(int i = 0; i < LoadAndStoreUnitNumber; i++){
                    string debug = $"- Reserve station Load Store[{i}] is {ExcutionUnits.LoadStoreunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
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
        static class ExcutionUnits{
            #region Excution Units
            static public excutionUnit[] ALUunits = new excutionUnit[ALUUnitNumber];
            static public excutionUnit[] Branchunits = new excutionUnit[BranchUnitNumber];
            static public excutionUnit[] LoadStoreunits = new excutionUnit[LoadAndStoreUnitNumber];
            #endregion
            static public void makeExcutionUnits(){
                for(int i = 0; i < ALUUnitNumber; i++){
                    excutionUnit newExcutionUnit = new excutionUnit{
                        numberOfCommandsInTheStation = 0,
                        cyclesBusyFor = 0,
                        historyResStation = "",
                        type = excutionUnitType.ALU,
                        resStation = new command[SizeOfReservationStation]
                    };
                    ALUunits[i] = newExcutionUnit;
                }
                for(int i = 0; i < BranchUnitNumber; i++){
                    excutionUnit newExcutionUnit = new excutionUnit{
                        numberOfCommandsInTheStation = 0,
                        cyclesBusyFor = 0,
                        historyResStation = "",
                        type = excutionUnitType.Branch,
                        resStation = new command[SizeOfReservationStation]
                    };
                    Branchunits[i] = newExcutionUnit;
                }
                for(int i = 0; i < LoadAndStoreUnitNumber; i++){
                    excutionUnit newExcutionUnit = new excutionUnit{
                        numberOfCommandsInTheStation = 0,
                        cyclesBusyFor = 0,
                        historyResStation = "",
                        type = excutionUnitType.LoadStore,
                        resStation = new command[SizeOfReservationStation]
                    };
                    LoadStoreunits[i] = newExcutionUnit;
                }
            }
            /*Excution Units*/
            static public void excutionUnitManager(int pipeName){
                string debug = String.Format($"Opcode recieved: {Pipe.pipes[pipeName].Opcode}, with Destination: {Pipe.pipes[pipeName].Destination}, regVal1:{Pipe.pipes[pipeName].valueRegisters[0]} and regVal2:{Pipe.pipes[pipeName].valueRegisters[1]}");
                DebugPrint(debug);
                //Do excution 
                command newCommand = new command{
                    opCode = Pipe.pipes[pipeName].Opcode, 
                    destination = Pipe.pipes[pipeName].Destination, 
                    value1 = Pipe.pipes[pipeName].valueRegisters[0], 
                    value2 = Pipe.pipes[pipeName].valueRegisters[1]
                };
                if(ReservationStationsUsed == true){
                    excutionUnitType type;
                    if(newCommand.opCode == "LDC" || newCommand.opCode == "LDC") type = excutionUnitType.LoadStore;
                    else if(newCommand.opCode == "BEQ" || newCommand.opCode == "BNE") type = excutionUnitType.Branch;
                    else type = excutionUnitType.ALU;
                    AssignToExcutionUnit(type,newCommand);
                }else{
                    ExcutionUnit(pipeName, false, newCommand);
                }
            }
            static void AssignToExcutionUnit(excutionUnitType type, command newCommand){
                //Just determines where to put the command
                excutionUnit[] units = new excutionUnit[0];
                int numberOfUnits = 0;
                int exUnitToBeGivenCommand = 0;
                int posResStation = 0;
                if(excutionUnitType.ALU == type){
                    units = ALUunits;
                    numberOfUnits = ALUUnitNumber;
                }
                else if(excutionUnitType.Branch == type){
                    units = Branchunits;
                    numberOfUnits = BranchUnitNumber;
                }
                else if(excutionUnitType.LoadStore == type){
                    units = LoadStoreunits;
                    numberOfUnits = LoadAndStoreUnitNumber;
                }
                for(int i = 0; i < numberOfUnits; i++){
                    //Start by assigning to the first ALU unit
                    if(i == 0) {
                        exUnitToBeGivenCommand = 0;
                        posResStation = units[i].numberOfCommandsInTheStation;
                    }
                    //Check to see if one has less commands in than the other
                    else {
                        if(units[i].numberOfCommandsInTheStation < posResStation){
                            exUnitToBeGivenCommand = i;
                            posResStation = units[i].numberOfCommandsInTheStation;
                        }
                    }
                }
                //Put command in RS
                units[exUnitToBeGivenCommand].resStation[posResStation] = newCommand;
                units[exUnitToBeGivenCommand].numberOfCommandsInTheStation++;
            }
            static public void ProcessReserveStations(){
                for(int i = 0; i < ALUUnitNumber; i++){
                    if(ALUunits[i].resStation[0].opCode != ""){
                        if(ALUunits[i].cyclesBusyFor == 0){
                            ExcutionUnit(i, true, ALUunits[i].resStation[0]);
                            PopLastCommandFromReserveStations(ref ALUunits[i]);
                        }
                        else ALUunits[i].cyclesBusyFor--;
                    } 
                }
                for(int i = 0; i < BranchUnitNumber; i++){
                    if(Branchunits[i].resStation[0].opCode != ""){
                        if(Branchunits[i].cyclesBusyFor == 0) {
                            ExcutionUnit(i, true, Branchunits[i].resStation[0]);
                            PopLastCommandFromReserveStations(ref Branchunits[i]);
                        }
                        else Branchunits[i].cyclesBusyFor--;
                    } 
                }
                for(int i = 0; i < LoadAndStoreUnitNumber; i++){
                    if(LoadStoreunits[i].resStation[0].opCode != ""){
                        if(LoadStoreunits[i].cyclesBusyFor == 0){
                            ExcutionUnit(i, true, LoadStoreunits[i].resStation[0]);
                            PopLastCommandFromReserveStations(ref LoadStoreunits[i]);
                        }
                        else LoadStoreunits[i].cyclesBusyFor--;
                    } 
                }
            }
            static void PopLastCommandFromReserveStations(ref excutionUnit unit){
                unit.historyResStation = unit.historyResStation + " | " + unit.resStation[0].opCode;
                for(int i = 1; i < SizeOfReservationStation; i++){
                    unit.resStation[i] = unit.resStation[i - 1]; 
                }
                unit.resStation[SizeOfReservationStation - 1] = new command{};
            }
            static void ExcutionUnit(int name, bool resStations, command Command){
                //Here's where we decide what to actually do
                //REGISTER COMMANDS
                if(Command.opCode == "LD"){
                    //Load Register via offset
                    //Sorted to ldc at decode so we shouldn't ever run this 
                    Console.WriteLine("ERROR ----- We have command LD where we should have LDC, maybe decode failed?");
                }
                if(Command.opCode == "LDC"){
                    //Load Register directly
                    loadDirectly(Command.destination, Command.value1);
                    if(resStations == true){
                        LoadStoreunits[name].cyclesBusyFor = loadAndStoreCycles;
                    } else {
                        //How long will the pipe be busy
                        Pipe.pipes[name].numCyclesBusyFor = loadAndStoreCycles;
                    }
                }

                //BRANCH COMMANDS
                if(Command.opCode == "BEQ"){
                    BranchEqual(Command.value1, Command.value2);
                }
                if(Command.opCode == "BNE"){
                    BranchNotEqual(Command.value1, Command.value2);
                }

                //ARTHEMETRIC
                if(Command.opCode == "ADDI"){
                    addiEU(Command.destination, Command.value1, Command.value2);
                }
                if(Command.opCode == "ADD"){
                    addEU(Command.destination, Command.value1, Command.value2);
                }
                if(Command.opCode == "SUB"){
                    subEU(Command.destination, Command.value1, Command.value2);
                }
                if(Command.opCode == "COMP"){
                    compare(Command.destination, Command.value1, Command.value2);
                }
                if(Command.opCode == "MUL"){
                    mulEU(Command.destination, Command.value1, Command.value2);
                    if(resStations == true){
                        ALUunits[name].cyclesBusyFor = multiplyCycles;
                    } else {
                        //How long will the pipe be busy
                        Pipe.pipes[name].numCyclesBusyFor = multiplyCycles;
                    }
                }
                if(Command.opCode == "DIV"){
                    divEU(Command.destination, Command.value1, Command.value2);
                    if(resStations == true){
                        ALUunits[name].cyclesBusyFor = divisionCycles;
                    } else {
                        //How long will the pipe be busy
                        Pipe.pipes[name].numCyclesBusyFor = divisionCycles;
                    }
                }
            }
            #region ALU processes
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
            static void mulEU(string r1, int r2, int r3){
                //MUL r1 = r2 * r3 
                int result = r2 * r3;
                //System.Console.WriteLine($"Multiplying {r2} to {r3}: Result {result}");
                Memory.PutValueInRegister(r1, result);
                //Write Back Debug
                DebugPrintWriteBack($"Mul Write Back {result}");
            }
            static void divEU(string r1, int r2, int r3){
                if(r3 == 0){
                    Console.WriteLine("TRIED TO DIVIDE BY ZERO COMMAND IGNORED!");
                    return;
                }
                //MUL r1 = r2 / r3 
                int result = r2 / r3;
                //System.Console.WriteLine($"Dividing {r2} to {r3}: Result {result} into {r1}");
                Memory.PutValueInRegister(r1, result);
                //Write Back Debug
                DebugPrintWriteBack($"Div Write Back {result}");
            }
            #endregion
            #region Branch processes
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
            #endregion
            #region Memory Processes
            static void loadDirectly(string r1, int r2){
                //Load r2's value into r1
                //Console.WriteLine($"Loading value {registesCurrentValue} into {r1}");
                Memory.PutValueInRegister(r1, r2);
                //Write Back Debug
                DebugPrintWriteBack($"Load Write Back: loaded {r2} into {r1}");
            }
            #endregion
            #region Debugging 
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
            #endregion
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
                    pipes[i].numCyclesBusyFor = 0;
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
                            //Debug assignment
                            if(PipeAssignmentDebug == true) Console.WriteLine($"Pipe {pipes[i].Name} has been given: {instructionList[ProgramCounter]}");

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
                    //Check to see if the pipe is still excuting 
                    if(Pipe.pipes[pipeName].numCyclesBusyFor == 0){
                        PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Waiting", pipeName);
                    } else {
                        PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Excute", pipeName);
                    }
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
                //We check to see if it's at the end of the excution (this is how different length running commands are handled)
                if(Pipe.pipes[pipeName].numCyclesBusyFor == 0){
                    //We excute
                    ExcutionUnits.excutionUnitManager(pipeName);
                } else {
                    Pipe.pipes[pipeName].numCyclesBusyFor--;
                }
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
