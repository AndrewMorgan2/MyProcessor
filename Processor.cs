using System;
using System.Collections.Generic;

namespace MyProcessor
{
    public struct command
    {
        public string assemblyCode;
        public string opCode;
        public string destination;
        public List<string> dependencies;
        public string valueString1;
        public string valueString2;
        public int value1;
        public int value2;
        public int PC;
        public int result;
        public int cycleCalculatedIn;
        public int specBranch;
    }
    public class Processor
    {
        #region Processor Stats
        public static bool runProcessor = true;
        public static int NumberOfPipes = 3;
        public static int SizeOfCache = 20;
        public static int NumberOfCache = 3;
        public static int ALUUnitNumber = 3;
        public static int BranchUnitNumber = 1;
        public static int LoadAndStoreUnitNumber = 2;
        public static int SizeOfReservationStation = 50;
        public static bool ReservationStationsUsed = true;
        public static bool UnifiedReservationStationsUsed = false;
        public static bool BranchPredictionUsed = false;
        public static int SizeOfReOrderBuffer = 50;
        //This is the number of cycles before we force quit (used to detect infinite loops in a very simple way)
        public static int CycleLimit = 1000;
        public static int ProgramCounter, ExcutionOrder, Totalcycles = 0;
        #endregion
        #region Number of cycles to do certain operations
        //All of these are extra cycles so 2 extra cycles means 3 total
        //ADD ONE TO ALL TO GET TOTAL CYCLES
        public static int divisionCycles = 9;
        public static int loadAndStoreCycles = 1;
        public static int multiplyCycles = 2;
        #endregion
        #region Counting Vars for benchmarking and debug bools
        public static bool RunTests = true;
        public static int testCaseToRun = 4;
        public static bool PipeDebug = false;
        public static bool MemoryDebug = false;
        public static bool ExcutionUnitDebug = false;
        public static bool WriteBackDebug = false;
        public static bool MemoryReadOut = false;
        public static bool ReserveStationReadOut = false;
        public static bool ReserveStationHistory = false;
        public static bool PipeAssignmentDebug = false;
        public static bool BranchPredictorDebug = false;
        public static bool ReOrderBufferDebug = false;
        public static bool ReOrderBufferDebugOutput = false;
        public static bool ReOrderBufferHistoryDebug = false;
        public static bool InfiniteLoopDetection = true;
        public static int waitingCycles, cacheMisses = 0;
        public static int[] cacheCalls = new int[NumberOfCache];
        #endregion
        public static string[] instructionList;
        static void Main()
        {
            Console.WriteLine("----------------  Starting   ----------------");
            if (RunTests == true) Console.WriteLine("---------------- Test Cases ----------------");
            RunProcessorCode(testCaseToRun);
        }
        //Actually Running the Processor
        static void RunProcessorCode(int testCaseToRun)
        {
            //Instatiate Pipes and Memory
            Pipe.makePipes();
            Memory.makeMemory();
            ExcutionUnits.makeExcutionUnits();
            ReOrderBuffer.makeReOrderBuffer();

            //Read in commands into a useable arrray
            instructionList = new string[0];
            if (RunTests == true)
            {
                string input = $"./tests/testCase{testCaseToRun}.txt";
                instructionList = System.IO.File.ReadAllLines(@input);
                //Load in data for binary search and bubble sort so that we get better instructions per cycle
                //Makes comparison easier 
                if (testCaseToRun == 4)
                {
                    int[] arr = { 17, 16, 18, 2, 4, 6, 19, 20, 8, 1, 15, 14, 13, 9, 3, 5, 7, 10, 11, 12 };
                    //int[] arr = { 1, 100, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 10, 11, 12 };
                    for (int i = 0; i < 20; i++)
                    {
                        Memory.PutValueInRegisterByInt(i, arr[i]);
                    }
                }
                if (testCaseToRun == 5)
                {
                    int[] arr = { 0, 2, 5, 8, 10, 11, 15, 17, 22, 23, 25, 27, 28, 30, 31, 40, 42, 45, 50, 64 };
                    for (int i = 0; i < 20; i++)
                    {
                        Memory.PutValueInRegisterByInt(i, arr[i]);
                    }
                }
            }
            else instructionList = System.IO.File.ReadAllLines(@"./assemblyCode.txt");
            System.Console.WriteLine($"Now lets run our processor with {instructionList.Length} commands");

            //Do we still have instruction to excute
            //Excute the instructions, that are still in pipes
            int pipesClear = 0;
            //Counting cycles for infiniteLoopDetection
            int cycles = 0;
            //RS trackers
            int totalReservationStations = 0;
            //If we use stations then track them and if we dont allow the processor to stop by making clearReservationStations < totalReservationStations always false
            if (ReservationStationsUsed == true) totalReservationStations = ALUUnitNumber + BranchUnitNumber + LoadAndStoreUnitNumber;
            int clearReservationStations = 0;
            //Checks to see if PC counter is still within bounds
            //Checks to see if pipes are clear
            //Checks to see if RS is clear 
            //Cheks to see if RoB is clear
            while (((instructionList.Length > ProgramCounter) || (pipesClear < NumberOfPipes) || (clearReservationStations < totalReservationStations) || (ReOrderBuffer.contenseOfReOrderBuffer == new List<command>(new command[SizeOfReOrderBuffer]))))
            {
                //Assign jobs to pipes
                Pipe.PipeAssignment(instructionList, ref ProgramCounter);
                if (ReservationStationsUsed == true)
                {
                    //Run all excution units
                    ExcutionUnits.ProcessReserveStations();
                }
                //Keep track of how many cycles are used
                Totalcycles++;

                //Debug per cycle
                //Console.WriteLine($"r20:{Memory.GetValueFromRegister("r20")}, r22:{Memory.GetValueFromRegister("r22")}, r23:{Memory.GetValueFromRegister("r23")}, r24:{Memory.GetValueFromRegister("r24")}");
                //Console.WriteLine($"PC: {ProgramCounter}");

                //Pipe Clear Detection
                pipesClear = 0;
                //Check if the pipes are clear
                for (int i = 0; NumberOfPipes > i; i++)
                {
                    //if the pipe is done or not used
                    if (Pipe.pipes[i].ActiveCommand == "Waiting" || Pipe.pipes[i].ActiveCommand == null)
                    {
                        pipesClear++;
                    }
                }
                cycles++;
                //Infinite loop only needs to be detected here (Andrew thinks this 15/3/2022)
                if (InfiniteLoopDetection == true)
                {
                    if (cycles > CycleLimit)
                    {
                        Console.WriteLine("CYCLE LIMIT REACHED, INFINITE CYLCE DETECTED");
                        break;
                    }
                }
                //Stop detection
                if (runProcessor == false)
                {
                    Console.WriteLine("CALLED STOP");
                    break;
                }

                if (ReservationStationsUsed == true)
                {
                    //RS clear check 
                    clearReservationStations = 0;
                    for (int i = 0; i < ALUUnitNumber; i++)
                    {
                        if (ExcutionUnits.ALUunits[i].numberOfCommandsInTheStation == 0) clearReservationStations++;
                    }
                    for (int i = 0; i < BranchUnitNumber; i++)
                    {
                        if (ExcutionUnits.Branchunits[i].numberOfCommandsInTheStation == 0) clearReservationStations++;
                    }
                    for (int i = 0; i < LoadAndStoreUnitNumber; i++)
                    {
                        if (ExcutionUnits.LoadStoreunits[i].numberOfCommandsInTheStation == 0) clearReservationStations++;
                    }
                }
            }
            PrintProcessorHistory();
        }
        //Prints all the information about the excution used (controlled by public booleans at the top)
        static void PrintProcessorHistory()
        {
            Console.WriteLine("----------------  Finished   ----------------");
            if (PipeDebug == true)
            {
                Console.WriteLine("----------------  Pipes history    ----------------");
                int actionsTaken = 0;
                for (int i = 0; NumberOfPipes > i; i++)
                {
                    if (Pipe.pipes[i].CommandHistory == null) System.Console.WriteLine($" --- Pipe {Pipe.pipes[i].Name} wasn't used");
                    else
                    {
                        System.Console.Write($" --- Pipe {Pipe.pipes[i].Name}'s History");
                        System.Console.WriteLine(Pipe.pipes[i].CommandHistory);
                        actionsTaken = actionsTaken + Pipe.pipes[i].instructionCycles;
                    }
                }
                Console.WriteLine($"Pipe Actions taken {actionsTaken}");
                Console.WriteLine($"Pipe Waits taken {waitingCycles}");
            }
            if (MemoryDebug == true)
            {
                Console.WriteLine("----------------   Memory stats    ----------------");
                for (int i = 0; NumberOfCache > i; i++)
                {
                    Console.WriteLine($"Cache calls for cache {i} = {cacheCalls[i]}");
                }
                Console.WriteLine($"Number of cache misses {cacheMisses}");
            }
            if (MemoryReadOut == true)
            {
                Console.WriteLine("----------------   Memory readOut    ----------------");
                string readOut = "";
                for (int i = 0; i < NumberOfCache; i++)
                {
                    readOut = readOut + $"Cache {i} contained: ";
                    for (int a = 0; a < SizeOfCache; a++)
                    {
                        readOut = readOut + /*$"r{a + (i*SizeOfCache)} " +*/ Memory.Regfile.caches[i].memory[a] + " | ";
                    }
                    readOut = readOut + "\n";
                }
                Console.WriteLine(readOut);
            }
            if (ReserveStationReadOut == true && ReservationStationsUsed == true)
            {
                Console.WriteLine("----------------   RS readOut    ----------------");
                for (int i = 0; i < ALUUnitNumber; i++)
                {
                    string debug;
                    debug = $"- Reserve station ALU[{i}] is {ExcutionUnits.ALUunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
                for (int i = 0; i < BranchUnitNumber; i++)
                {
                    string debug;
                    debug = $"- Reserve station Branch[{i + ALUUnitNumber}] is {ExcutionUnits.Branchunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
                for (int i = 0; i < LoadAndStoreUnitNumber; i++)
                {
                    string debug;
                    debug = $"- Reserve station Load Store[{i + ALUUnitNumber + BranchUnitNumber}] is {ExcutionUnits.LoadStoreunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
            }
            if (ReserveStationHistory == true && ReservationStationsUsed == true)
            {
                Console.WriteLine("----------------   RS history    ----------------");
                for (int i = 0; i < ALUUnitNumber; i++)
                {
                    string debug;
                    debug = $"- Reserve station ALU[{i}] is {ExcutionUnits.ALUunits[i].excutionHistory}";
                    Console.WriteLine(debug);
                }
                for (int i = 0; i < BranchUnitNumber; i++)
                {
                    string debug;
                    debug = $"- Reserve station Branch[{i}] is {ExcutionUnits.Branchunits[i].excutionHistory}";
                    Console.WriteLine(debug);
                }
                for (int i = 0; i < LoadAndStoreUnitNumber; i++)
                {
                    string debug;
                    debug = $"- Reserve station Load Store[{i}] is {ExcutionUnits.LoadStoreunits[i].excutionHistory}";
                    Console.WriteLine(debug);
                }
            }
            if (ReOrderBufferHistoryDebug == true)
            {
                ReOrderBuffer.PrintOutReOrderBufferHistory();
            }
            if (RunTests == true)
            {
                Console.WriteLine("---------------- Test Results ----------------");
                if (testCaseToRun == 1)
                {
                    Console.WriteLine($"result:{Memory.GetValueFromRegister("r2")} input:{Memory.GetValueFromRegister("r3")} loopCounterI:{Memory.GetValueFromRegister("r4")} For n!");
                    Console.WriteLine($"Test Result: {(Memory.GetValueFromRegister("r2") == Factorial(Memory.GetValueFromRegister("r3")))}");
                }
                else if (testCaseToRun == 2)
                {
                    Console.WriteLine($"result:{Memory.GetValueFromRegister("r2")} input:{Memory.GetValueFromRegister("r4")} loopCounterI:{Memory.GetValueFromRegister("r3")} For {Memory.GetValueFromRegister("r4")}th Fibonacci");
                    Console.WriteLine($"Test Result: {(Memory.GetValueFromRegister("r2") == fibonacci(Memory.GetValueFromRegister("r4")))}");
                }
                else if (testCaseToRun == 3)
                {
                    Console.WriteLine($"loopCounterI:{Memory.GetValueFromRegister("r5")} a:{Memory.GetValueFromRegister("r0")}, b:{Memory.GetValueFromRegister("r1")}, c:{Memory.GetValueFromRegister("r2")}, d:{Memory.GetValueFromRegister("r3")},  e:{Memory.GetValueFromRegister("r4")}");
                    Console.WriteLine($"Test Result: {Memory.GetValueFromRegister("r0") == 52} | {Memory.GetValueFromRegister("r1") == 54} | {Memory.GetValueFromRegister("r2") == 55} | {Memory.GetValueFromRegister("r3") == 58} | {Memory.GetValueFromRegister("r4") == 59}");
                }
                else if (testCaseToRun == 4)
                {
                    string Output = "Output";
                    for (int i = 0; i < 20; i++) Output = Output + ", " + Memory.GetValueFromRegister($"r{i}");
                    int[] arr = { 17, 16, 18, 2, 4, 6, 19, 20, 8, 1, 15, 14, 13, 9, 3, 5, 7, 10, 11, 12 };
                    string input = "Input";
                    foreach (int x in arr) input = input + ", " + x.ToString();
                    Console.WriteLine($"Test Input: {input}");
                    Console.WriteLine($"Test Result: {Output}");
                    Console.WriteLine($"index: {Memory.GetValueFromRegister("r20")} Result for n-1 comp i: {Memory.GetValueFromRegister("r30")} arr[i]:{Memory.GetValueFromRegister("r22")} arr[i+1]:{Memory.GetValueFromRegister("r23")}");
                    foreach (command comm in ReOrderBuffer.contenseOfReOrderBuffer)
                    {
                        if (comm.Equals(new command { })) break;
                        Console.WriteLine($"{comm.assemblyCode}");
                    }
                }
                else if (testCaseToRun == 5)
                {
                    //add check to see if binary search works
                    Console.WriteLine($"Test Result: {Memory.GetValueFromRegister("r20")} is in position {Memory.GetValueFromRegister("r24")} in the array");
                    Console.WriteLine($"With {Memory.GetValueFromRegister("r30")} cuts");
                }
            }
            Console.WriteLine("---------------- Key Info ----------------");
            Console.WriteLine($"RoB at {ReOrderBuffer.ShadowProgramCounter} | ExcutionOrder at {ExcutionOrder} | PC at {ProgramCounter} | Total cycles taken to complete the program {Totalcycles} | Instructions per cycle {ExcutionOrder} / {Totalcycles} ");
        }
        //Gets next part of the instruction eg (opcode/ register/ int/)
        public static string getNextPartFromText(string command)
        {
            string opcode = "";
            //check if the first x chars are empty spaces
            int numberOfSpaces = 0;
            foreach (char character in command)
            {
                if (character == ' ')
                {
                    numberOfSpaces++;
                }
                else break;
            }
            //Now we can actually get the next part
            command = command.Remove(0, numberOfSpaces);
            foreach (char character in command)
            {
                if (character != ' ')
                {
                    opcode = opcode + character;
                }
                else return opcode;
            }
            return opcode;
        }
        //Get Fibonacci number to check that processor give right answer
        static int fibonacci(int n)
        {
            // initialize first and second terms
            int t1 = 0, t2 = 1;

            // initialize the next term (3rd term)
            int nextTerm = t1 + t2;

            // print 3rd to nth terms
            for (int i = 3; i <= n; ++i)
            {
                t1 = t2;
                t2 = nextTerm;
                nextTerm = t1 + t2;
            }

            return nextTerm;
        }
        //Get Factorial to check that processor give right answer
        static int Factorial(int f)
        {
            if (f == 0)
                return 1;
            else
                return f * Factorial(f - 1);
        }
    }
}
