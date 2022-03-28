﻿using System;
using System.Collections.Generic;

namespace MyProcessor
{
    public struct command
    {
        public string opCode;
        public string destination;
        public List<string> dependencies;
        public int value1;
        public int value2;
        public int issuedOrder;
        public int result;
    }
    public class Processor
    {
        #region Processor Stats
        public static int NumberOfPipes = 3;
        public static int SizeOfCache = 10;
        public static int NumberOfCache = 3;
        public static int ALUUnitNumber = 3;
        public static int BranchUnitNumber = 1;
        public static int LoadAndStoreUnitNumber = 1;
        public static int SizeOfReservationStation = 3;
        public static bool ReservationStationsUsed = true;
        public static bool UnifiedReservationStationsUsed = false;
        public static int SizeOfReOrderBuffer = 20;
        //This is the number of cycles before we force quit (used to detect infinite loops in a very simple way)
        public static int CycleLimit = 100;
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
        public static bool PipeDebug = false;
        public static bool MemoryDebug = false;
        public static bool ExcutionUnitDebug = false;
        public static bool WriteBackDebug = false;
        public static bool MemoryReadOut = false;
        public static bool ReserveStationReadOut = false;
        public static bool ReserveStationHistory = false;
        public static bool PipeAssignmentDebug = false;
        public static bool ReOrderBufferDebug = false;
        public static bool ReOrderBufferDebugOutput = false;
        public static bool ReOrderBufferHistoryDebug = false;
        public static bool InfiniteLoopDetection = true;
        public static int waitingCycles, cacheMisses = 0;
        public static int[] cacheCalls = new int[NumberOfCache];
        #endregion
        static void Main(string[] args)
        {
            Console.WriteLine("----------------  Starting   ----------------");
            #region Instatiate pipes, memory, reorder buffer and reservation stations
            //Instatiate Pipes and Memory
            Pipe.makePipes();
            Memory.makeMemory();
            ExcutionUnits.makeExcutionUnits();
            ReOrderBuffer.makeReOrderBuffer();
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
                //Keep track of how many cycles are used
                Totalcycles++;
            }
            Console.WriteLine("---------------- All instructions have been fetched! ----------------");
            
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
                //Keep track of how many cycles are used
                Totalcycles++;

                pipesClear = 0;
                //Check if the pipes are clear
                for(int i = 0; NumberOfPipes > i; i++){
                    //if the pipe is done or not used
                    if(Pipe.pipes[i].ActiveCommand == "Waiting" || Pipe.pipes[i].ActiveCommand == null){
                        pipesClear++;
                    }
                }
                cycles ++;
                //Infinite loop only needs to be detected here (Andrew thinks this 15/3/2022)
                if(InfiniteLoopDetection == true){
                    if(cycles > CycleLimit) {
                        Console.WriteLine("CYCLE LIMIT REACHED, INFINITE CYLCE DETECTED");
                        break;
                    }
                }
            }

            //We excute if we still have commands in the reservation stations 
            if(ReservationStationsUsed){
                int totalReservationStations = ALUUnitNumber + BranchUnitNumber + LoadAndStoreUnitNumber;
                int clearReservationStations = 0;
                while(clearReservationStations < totalReservationStations){
                    clearReservationStations = 0;
                    for(int i = 0; i < ALUUnitNumber; i++){
                        if(ExcutionUnits.ALUunits[i].numberOfCommandsInTheStation == 0) clearReservationStations++;
                    }
                    for(int i = 0; i < BranchUnitNumber; i++){
                        if(ExcutionUnits.Branchunits[i].numberOfCommandsInTheStation == 0) clearReservationStations++;
                    }
                    for(int i = 0; i < LoadAndStoreUnitNumber; i++){
                        if(ExcutionUnits.LoadStoreunits[i].numberOfCommandsInTheStation == 0) clearReservationStations++;
                    }
                    ExcutionUnits.ProcessReserveStations();
                    //Keep track of how many cycles are used
                    Totalcycles++;
                }
            }
            #endregion

            #region Printing Processor History
            Console.WriteLine("----------------  Finished   ----------------");
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
                    string debug;
                    debug = $"- Reserve station ALU[{i}] is {ExcutionUnits.ALUunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
                for(int i = 0; i < BranchUnitNumber; i++){
                    string debug;
                    debug = $"- Reserve station Branch[{i + ALUUnitNumber}] is {ExcutionUnits.Branchunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
                for(int i = 0; i < LoadAndStoreUnitNumber; i++){
                    string debug;
                    debug = $"- Reserve station Load Store[{i + ALUUnitNumber + BranchUnitNumber}] is {ExcutionUnits.LoadStoreunits[i].historyResStation}";
                    Console.WriteLine(debug);
                }
            }
            if(ReserveStationHistory == true && ReservationStationsUsed == true){
                Console.WriteLine("----------------   RS history    ----------------");
                 for(int i = 0; i < ALUUnitNumber; i++){
                    string debug;
                    debug = $"- Reserve station ALU[{i}] is {ExcutionUnits.ALUunits[i].excutionHistory}";
                    Console.WriteLine(debug);
                }
                for(int i = 0; i < BranchUnitNumber; i++){
                    string debug;
                    debug = $"- Reserve station Branch[{i}] is {ExcutionUnits.Branchunits[i].excutionHistory}";
                    Console.WriteLine(debug);
                }
                for(int i = 0; i < LoadAndStoreUnitNumber; i++){
                    string debug;
                    debug = $"- Reserve station Load Store[{i }] is {ExcutionUnits.LoadStoreunits[i].excutionHistory}";
                    Console.WriteLine(debug);
                }
            }
            if(ReOrderBufferHistoryDebug == true){
                ReOrderBuffer.PrintOutReOrderBufferHistory();
            }
            Console.WriteLine($"RoB at {ReOrderBuffer.LastExcutionOrder} while PC at {ExcutionOrder}");
            Console.WriteLine($"Total cycles taken to complete the program {Totalcycles}");
            #endregion
        }

        //Gets next part of the instruction eg (opcode/ register/ int/)
        public static string getNextPartFromText(string command){
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
    }
}
