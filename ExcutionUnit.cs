using System;
using System.Collections.Generic;
using static MyProcessor.Processor;
using command = MyProcessor.command;
public static class ExcutionUnits
{
    #region Data Structs and Excution Units
    public struct excutionUnit
    {
        public int numberOfCommandsInTheStation;
        public int cyclesBusyFor;
        public excutionUnitType type;
        public command[] resStation;
        public string historyResStation;
        public bool busy;
        public string excutionHistory;
    }
    public enum excutionUnitType
    {
        ALU, Branch, LoadStore, Unified
    }
    public static excutionUnit[] ALUunits = new excutionUnit[ALUUnitNumber];
    public static excutionUnit[] Branchunits = new excutionUnit[BranchUnitNumber];
    public static excutionUnit[] LoadStoreunits = new excutionUnit[LoadAndStoreUnitNumber];
    public static command[] UnifiedReserveStations = new command[SizeOfReservationStation];
    #endregion
    //Called at the start to make units
    static public void makeExcutionUnits()
    {
        for (int i = 0; i < ALUUnitNumber; i++)
        {
            excutionUnit newExcutionUnit = new excutionUnit
            {
                numberOfCommandsInTheStation = 0,
                cyclesBusyFor = 0,
                historyResStation = "",
                excutionHistory = "",
                type = excutionUnitType.ALU,
                resStation = new command[SizeOfReservationStation]
            };
            ALUunits[i] = newExcutionUnit;
        }
        for (int i = 0; i < BranchUnitNumber; i++)
        {
            excutionUnit newExcutionUnit = new excutionUnit
            {
                numberOfCommandsInTheStation = 0,
                cyclesBusyFor = 0,
                historyResStation = "",
                excutionHistory = "",
                type = excutionUnitType.Branch,
                resStation = new command[SizeOfReservationStation]
            };
            Branchunits[i] = newExcutionUnit;
        }
        for (int i = 0; i < LoadAndStoreUnitNumber; i++)
        {
            excutionUnit newExcutionUnit = new excutionUnit
            {
                numberOfCommandsInTheStation = 0,
                cyclesBusyFor = 0,
                historyResStation = "",
                excutionHistory = "",
                type = excutionUnitType.LoadStore,
                resStation = new command[SizeOfReservationStation]
            };
            LoadStoreunits[i] = newExcutionUnit;
        }
    }
    //Generate command and then call AssignToExcutionUnit
    static public void excutionUnitManager(int pipeName)
    {
        string debug = String.Format($"Opcode recieved: {Pipe.pipes[pipeName].Opcode}, with Destination: {Pipe.pipes[pipeName].Destination}, regVal1:{Pipe.pipes[pipeName].valueRegisters[0]} and regVal2:{Pipe.pipes[pipeName].valueRegisters[1]}");
        DebugPrint(debug);
        //Do excution 
        command newCommand = new command
        {
            assemblyCode = Pipe.pipes[pipeName].assemblyCode,
            opCode = Pipe.pipes[pipeName].Opcode,
            destination = Pipe.pipes[pipeName].Destination,
            valueString1 = Pipe.pipes[pipeName].valueRegisters[0],
            valueString2 = Pipe.pipes[pipeName].valueRegisters[1],
            dependencies = new List<string>(),
            PC = Pipe.pipes[pipeName].PC,
            cycleCalculatedIn = 0,
            specBranch = Pipe.pipes[pipeName].specBranch,
            result = 0
        };
        if (ReservationStationsUsed == true)
        {
            AssignToExcutionUnit(newCommand);
        }
        else
        {
            if (Pipe.pipes[pipeName].busy == true)
            {
                if (Pipe.pipes[pipeName].numCyclesBusyFor == 0)
                {
                    //We excute
                    ExcutionAfterTime(pipeName, false, ref newCommand, ref Branchunits[0]);
                }
                else
                {
                    Pipe.pipes[pipeName].numCyclesBusyFor--;
                }
            }
            else
            {
                ExcutionUnit(pipeName, false, ref newCommand);
            }
        }
    }
    //Give the command to the reservation station depending on the type of opcode recieved
    static public void AssignToExcutionUnit(command newCommand)
    {
        //Get values
        excutionUnitType type;
        if (UnifiedReservationStationsUsed == true) type = excutionUnitType.Unified;
        else if (newCommand.opCode == "LDC" || newCommand.opCode == "STR" || newCommand.opCode == "LD") type = excutionUnitType.LoadStore;
        else if (newCommand.opCode == "BEQ" || newCommand.opCode == "BNE" || newCommand.opCode == "JUMP") type = excutionUnitType.Branch;
        else type = excutionUnitType.ALU;
        //Just determines where to put the command
        excutionUnit[] units = new excutionUnit[0];
        int numberOfUnits = 0;
        int exUnitToBeGivenCommand = 0;
        int posResStation = 0;
        //Assign number of units and units
        if (excutionUnitType.ALU == type)
        {
            units = ALUunits;
            numberOfUnits = ALUUnitNumber;
        }
        else if (excutionUnitType.Branch == type)
        {
            units = Branchunits;
            numberOfUnits = BranchUnitNumber;
        }
        else if (excutionUnitType.LoadStore == type)
        {
            units = LoadStoreunits;
            numberOfUnits = LoadAndStoreUnitNumber;
        }

        if (excutionUnitType.Unified == type)
        {
            for (int i = 0; i < SizeOfReservationStation; i++)
            {
                if (UnifiedReserveStations[i].Equals(new command { }))
                {
                    UnifiedReserveStations[i] = newCommand;
                    break;
                }
            }
        }
        else
        {
            posResStation = LocateCorrectReserveStation(numberOfUnits, ref units, ref exUnitToBeGivenCommand);
            //Put command in RS
            if (posResStation >= SizeOfReservationStation)
            {
                Console.WriteLine("RESERVATION STATION IS TOO FULL");
                Pipe.sentBackCommands.Add(newCommand);
            }
            else
            {
                units[exUnitToBeGivenCommand].resStation[posResStation] = newCommand;
                units[exUnitToBeGivenCommand].numberOfCommandsInTheStation++;
            }
        }
    }
    static int LocateCorrectReserveStation(int unitNumber, ref excutionUnit[] units, ref int exUnitToBeGivenCommand)
    {
        int posResStation = 0;
        for (int i = 0; i < unitNumber; i++)
        {
            if (i == 0)
            {
                exUnitToBeGivenCommand = 0;
                posResStation = units[i].numberOfCommandsInTheStation;
            }
            else
            {
                if (units[i].numberOfCommandsInTheStation < posResStation)
                {
                    exUnitToBeGivenCommand = i;
                    posResStation = units[i].numberOfCommandsInTheStation;
                }
            }
        }
        return posResStation;
    }
    //Called once per cycle calling Process unit for all types of excution unit
    static public void ProcessReserveStations()
    {
        if (UnifiedReservationStationsUsed == true)
        {
            UnifiedReserveStationDistributeCommands();
        }
        ProcessUnit(ref ALUunits);
        ProcessUnit(ref Branchunits);
        ProcessUnit(ref LoadStoreunits);
    }
    //If we use Unified RS then we need to send commands to the correct excution unit
    static void UnifiedReserveStationDistributeCommands()
    {
        command[] newUnifiedReservation = new command[SizeOfReservationStation];
        int placeInNewUnifiedReservationStation = 0;
        //Cycle though the unified reservation station
        for (int b = 0; b < SizeOfReservationStation; b++)
        {
            //If it's empty do nothing 
            if (UnifiedReserveStations[b].Equals(new command { })) break;

            command newCommand = UnifiedReserveStations[b];
            int unitNumber = 0;
            bool sentToExcutionUnit = false;
            excutionUnit[] units = new excutionUnit[0];
            //Decide where teh command should go
            if (newCommand.opCode == "LDC")
            {
                units = LoadStoreunits;
                unitNumber = LoadAndStoreUnitNumber;
            }
            else if (newCommand.opCode == "BEQ" || newCommand.opCode == "BNE")
            {
                units = Branchunits;
                unitNumber = BranchUnitNumber;
            }
            else
            {
                units = ALUunits;
                unitNumber = ALUUnitNumber;
            }
            //See if there's a space for the command
            for (int a = 0; a < unitNumber; a++)
            {
                if (units[a].busy == false)
                {
                    //Console.WriteLine($"Command from Unified RS to Excution Unit: {newCommand.opCode}");
                    units[a].resStation[0] = newCommand;
                    units[a].numberOfCommandsInTheStation++;
                    sentToExcutionUnit = true;
                    break;
                }
            }
            //Recreate a list of unsent commands
            if (sentToExcutionUnit == false)
            {
                newUnifiedReservation[placeInNewUnifiedReservationStation] = newCommand;
                placeInNewUnifiedReservationStation++;
            }
        }
        //Redefine Unified RS with commands not taken out 
        UnifiedReserveStations = newUnifiedReservation;
    }
    //Adds to history of units and also progesses the state of commands that are being done (number of cycles that it's busy for -1)
    static void ProcessUnit(ref excutionUnit[] units)
    {
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i].numberOfCommandsInTheStation > 0)
            {
                if (units[i].busy == false)
                {
                    units[i].excutionHistory = units[i].excutionHistory + " | Quick Run " + units[i].resStation[0].opCode;
                    ExcutionUnit(i, true, ref units[i].resStation[0]);
                    if (units[i].busy == false) PopLastCommandFromReserveStations(ref units[i]);
                }
                else
                {
                    if (units[i].cyclesBusyFor == 0)
                    {
                        units[i].excutionHistory = units[i].excutionHistory + " | Finishing " + units[i].resStation[0].opCode;
                        ExcutionAfterTime(i, true, ref units[i].resStation[0], ref units[0]);
                        PopLastCommandFromReserveStations(ref units[i]);
                    }
                    else
                    {
                        units[i].excutionHistory = units[i].excutionHistory + " | Doing " + units[i].resStation[0].opCode;
                        units[i].cyclesBusyFor--;
                    }
                }
            }
        }
    }
    static void PopLastCommandFromReserveStations(ref excutionUnit unit)
    {
        if (unit.resStation[0].Equals(new command { })) return;

        unit.historyResStation = unit.historyResStation + unit.resStation[0].opCode + " | ";
        for (int i = 0; i < SizeOfReservationStation - 1; i++)
        {
            unit.resStation[i] = unit.resStation[i + 1];
        }
        unit.resStation[SizeOfReservationStation - 1] = new command { };
        unit.numberOfCommandsInTheStation--;
        //The unit is now not busy 
        unit.busy =false;
    }
    //Both functions below sort though the opCode and gives it to the correct Excution Unit
    static void ExcutionUnit(int name, bool resStations, ref command Command)
    {
        GetValues(ref Command);
        Command.cycleCalculatedIn = Totalcycles;
        //Get Values from strings 

        //Here's where we decide what to actually do
        //REGISTER COMMANDS
        if (Command.opCode == "LDC" || Command.opCode == "STR" || Command.opCode == "LD")
        {
            //Load Register directly
            if (resStations == true)
            {
                LoadStoreunits[name].cyclesBusyFor = loadAndStoreCycles;
                LoadStoreunits[name].busy = true;
            }
            else
            {
                //How long will the pipe be busy
                Pipe.pipes[name].numCyclesBusyFor = loadAndStoreCycles;
                Pipe.pipes[name].busy = true;
            }
        }

        //BRANCH COMMANDS
        else if (Command.opCode == "BEQ")
        {
            BranchEqual(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "BNE")
        {
            BranchNotEqual(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "JUMP")
        {
            ReOrderBuffer.addCommand(Command);
        }

        //ARTHEMETRIC
        else if (Command.opCode == "ADDI")
        {
            addiEU(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "ADD")
        {
            addEU(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "SUB")
        {
            subEU(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "COMP")
        {
            compare(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "MUL")
        {
            if (resStations == true)
            {
                ALUunits[name].cyclesBusyFor = multiplyCycles;
                ALUunits[name].busy = true;
            }
            else
            {
                //How long will the pipe be busy
                Pipe.pipes[name].numCyclesBusyFor = multiplyCycles;
                Pipe.pipes[name].busy = true;
            }
        }
        else if (Command.opCode == "DIV")
        {
            if (resStations == true)
            {
                ALUunits[name].cyclesBusyFor = divisionCycles;
                ALUunits[name].busy = true;
            }
            else
            {
                //How long will the pipe be busy
                Pipe.pipes[name].numCyclesBusyFor = divisionCycles;
                Pipe.pipes[name].busy = true;
            }
        }
        //NOP
        else if (Command.opCode == "NOP") ReOrderBuffer.addCommand(Command);
        else
        {
            Console.WriteLine($"EXCUTION UNIT RECIEVED UNREADABLE OPCODE {Command.opCode}");
        }
    }
    static public void ExcutionAfterTime(int name, bool resStations, ref command Command, ref excutionUnit unit)
    {
        GetValues(ref Command);
        Command.cycleCalculatedIn = Totalcycles;
        if (resStations == true)
        {
            unit.busy = false;
        }
        else
        {
            Pipe.pipes[name].busy = false;
        }
        if (Command.opCode == "LDC")
        {
            //Load Register directly
            loadDirectly(Command.destination, Command.value1, ref Command);
        }
        else if (Command.opCode == "LD")
        {
            //Load Register via index
            loadViaIndex(Command.destination, Command.valueString1, ref Command);
        }
        else if (Command.opCode == "STR")
        {
            //Store values 
            store(Command.value2, Command.value1, ref Command);
        }
        else if (Command.opCode == "MUL")
        {
            mulEU(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else if (Command.opCode == "DIV")
        {
            divEU(Command.destination, Command.value1, Command.value2, ref Command);
        }
        else Console.WriteLine($"DETECTED NONE LONG EXCUTION FUNCTION ENTERING EXCUTION AFTER TIME {Command.opCode} {name} {Totalcycles} {Pipe.pipes[name].busy}");
    }
    static void GetValues(ref command Command)
    {
        if (Command.opCode == "NOP" || Command.opCode == "JUMP") return;
        //Get value from register here
        //We leave r2 as a register

        if (Command.valueString1.Contains('r') == true)
        {
            Command.value1 = Memory.GetValueFromRegister(Command.valueString1);
            Command.dependencies.Add(Command.valueString1);
        }
        else Command.value1 = Int32.Parse(Command.valueString1);

        //Check to see if they're an opCode with another value
        if(Command.opCode == "LDC" || Command.opCode == "STR" || Command.opCode == "LD") return;
        //Get value from register here (if possible)
        if (Command.valueString2.Contains('r') == true)
        {
            Command.value2 = Memory.GetValueFromRegister(Command.valueString2);
            Command.dependencies.Add(Command.valueString2);
        }
        else Command.value2 = Int32.Parse(Command.valueString2);

    }
    //These functions calculate results and send them to the reorder buffer
    #region ALU processes
    static void addEU(string r1, int r2, int r3, ref command commandPassed)
    {
        //ADD r1 = r2 + r3
        int result = r2 + r3;
        //System.Console.WriteLine($"Adding {r2} to {r3}: Result {result}");
        commandPassed.result = result;
        ReOrderBuffer.addCommand(commandPassed);

        //Write Back Debug
        DebugPrintWriteBack($"Add Write Back {result}");
    }
    static void subEU(string r1, int r2, int r3, ref command commandPassed)
    {
        //SUB r1 = r2 - r3 
        int result = r2 - r3;
        //System.Console.WriteLine($"Subtracting {r2} to {r3}: Result {result}");
        commandPassed.result = result;
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"Sub Write Back {result}");
    }
    static void addiEU(string r1, int r2, int r3, ref command commandPassed)
    {
        //ADDI r1 increamented by r2(value)
        int result = r2 + r3;
        //System.Console.WriteLine($"Adding register {r1} to {x}: Result {result}");
        commandPassed.result = result;
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"AddI Write Back {result}");
    }
    static void compare(string r1, int r2, int r3, ref command commandPassed)
    {
        if (r2 < r3)
        {
            commandPassed.result = -1;
            ReOrderBuffer.addCommand(commandPassed);
            //Write Back Debug
            DebugPrintWriteBack($"Compare Write Back: loaded {-1} into {r1}");
        }
        else if (r2 > r3)
        {
            commandPassed.result = 1;
            ReOrderBuffer.addCommand(commandPassed);
            //Write Back Debug
            DebugPrintWriteBack($"Compare Write Back: loaded {1} into {r1}");
        }
        else
        {
            commandPassed.result = 0;
            ReOrderBuffer.addCommand(commandPassed);
            //Write Back Debug
            DebugPrintWriteBack($"Compare Write Back: loaded {0} into {r1}");
        }
    }
    static void mulEU(string r1, int r2, int r3, ref command commandPassed)
    {
        //MUL r1 = r2 * r3 
        int result = r2 * r3;
        //System.Console.WriteLine($"Multiplying {r2} to {r3}: Result {result}");
        commandPassed.result = result;
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"Mul Write Back {result} from {r2} and {r3}");
    }
    static void divEU(string r1, int r2, int r3, ref command commandPassed)
    {
        if (r3 == 0)
        {
            Console.WriteLine("TRIED TO DIVIDE BY ZERO COMMAND IGNORED!");
            return;
        }
        //MUL r1 = r2 / r3 
        int result =(int)Math.Round((decimal)(r2 / r3));
        //System.Console.WriteLine($"Dividing {r2} to {r3}: Result {result} into {r1}");
        commandPassed.result = result;
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"Div Write Back {result}");
    }
    #endregion
    #region Branch processes
    static void BranchEqual(string destination, int value1, int value2, ref command commandPassed)
    {
        if (value1 == value2)
        {
            commandPassed.result = 1;
            ReOrderBuffer.addCommand(commandPassed);
            DebugPrintWriteBack($"BranchEqual Write Back: change PC to {destination}");
        }
        else
        {
            //Even if we dont take the branch we should tell the RoB that an action should be taken
            commandPassed.result = 0;
            ReOrderBuffer.addCommand(commandPassed);
            DebugPrintWriteBack($"BranchEqual Write Back: didnt change PC");
        }
    }
    static void BranchNotEqual(string destination, int value1, int value2, ref command commandPassed)
    {
        if (value1 != value2)
        {
            commandPassed.result = 1;
            ReOrderBuffer.addCommand(commandPassed);
            DebugPrintWriteBack($"BranchNotEqual Write Back: changed PC to {destination}");
        }
        else
        {
            //Even if we dont take the branch we should tell the RoB that an action should be taken
            commandPassed.result = 0;
            ReOrderBuffer.addCommand(commandPassed);
            DebugPrintWriteBack($"BranchNotEqual Write Back: didnt change PC");
        }
    }
    #endregion
    #region Memory Processes
    static void loadDirectly(string r1, int r2, ref command commandPassed)
    {
        //Load r2's value into r1
        //Console.WriteLine($"Loading value {registesCurrentValue} into {r1}");
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"Load Write Back: loaded {r2} into {r1}");
    }
    static void loadViaIndex(string r1, string r2, ref command commandPassed)
    {
        //Load r2's value into r1
        //Console.WriteLine($"Loading value {registesCurrentValue} into {r1}");
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"Load Index Write Back: value in r{Memory.GetValueFromRegister(r2)} now also in {r1}");
    }
    static void store(int r1, int r2, ref command commandPassed){
        //Store value
        ReOrderBuffer.addCommand(commandPassed);
        //Write Back Debug
        DebugPrintWriteBack($"Swapping values {r1} and {r2}");
    }
    #endregion

    #region Debugging 
    static void DebugPrint(string debugPrint)
    {
        if (ExcutionUnitDebug == true)
        {
            Console.WriteLine(debugPrint);
        }
    }
    static void DebugPrintWriteBack(string debugPrint)
    {
        if (WriteBackDebug == true)
        {
            Console.WriteLine(debugPrint);
        }
    }
    #endregion
}