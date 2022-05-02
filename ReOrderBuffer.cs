using System;
using System.Collections.Generic;
using static MyProcessor.Processor;
using command = MyProcessor.command;
static class ReOrderBuffer
{
    #region static public Vars
    static public List<command> contenseOfReOrderBuffer, speculativeCommands, predictions;
    static private List<dTracker> DependencyTracker;
    static public int numberOfCommandsSentBack, ShadowProgramCounter = 0;
    static private string HistoryInput, HistoryOutput;
    struct dTracker
    {
        public string registerName;
        public int cycleLastEditedIn;
    }
    #endregion
    //Called at start to make the RoB
    static public void makeReOrderBuffer()
    {
        contenseOfReOrderBuffer = new List<command>(new command[SizeOfReOrderBuffer]);
        DependencyTracker = new List<dTracker>();
        speculativeCommands = new List<command>();
        predictions = new List<command>();
        HistoryOutput = "";
        HistoryInput = "";
    }
    //We are going to check to see if we should commit or we should keep in the reorder buffer
    static public void addCommand(command newCommand)
    {
        //If speculative command then we add to a list
        //0 means it's not speculation
        if (newCommand.specBranch != 0)
        {

            foreach (command cmd in speculativeCommands)
            {
                //Console.WriteLine($"{newCommand.opCode}");
                if (cmd.assemblyCode == newCommand.assemblyCode) return;
            }
            speculativeCommands.Add(newCommand);
            BranchPrediction.BranchDebug($"Added {newCommand.opCode} to spec branch in RoB");
            return;
        }

        //DebugLog($"Recieved {newCommand.opCode} at {newCommand.PC} with shadow:{ShadowProgramCounter}");
        //No Duplicates (command has program counter)
        //Console.WriteLine(newCommand.assemblyCode);
        foreach (command comm in contenseOfReOrderBuffer)
        {
            if (comm.Equals(new command { })) break;
            else if (comm.assemblyCode == newCommand.assemblyCode) return;
        }
        //If we load in an old command then ignore it 
        if(newCommand.PC < ShadowProgramCounter) {
            //Console.WriteLine($"old commands sent to ROB");
            return;
        }
        DebugLog($"Added {newCommand.assemblyCode} at {newCommand.PC} with shadow:{ShadowProgramCounter}");

        //DebugLog($"Recieved command {newCommand.opCode}");
        HistoryInput = HistoryInput + newCommand.opCode + " | ";
        //Shadow PC makes sure we keep order after OoO excution 
        if (ShadowProgramCounter == newCommand.PC)
        {
            DebugLog($"{newCommand.opCode} straight in {newCommand.PC}");
            //Send to commit unit
            Commit(ref newCommand);
        }
        else
        {
            //Add to list in correct place 
            for (int i = 0; i < SizeOfReOrderBuffer; i++)
            {
                //if empty place or issued earlier
                if (contenseOfReOrderBuffer[i].Equals(new command { }) || newCommand.PC < contenseOfReOrderBuffer[i].PC)
                {
                    //Check to see if the buffer is full
                    if (!contenseOfReOrderBuffer[SizeOfReOrderBuffer - 1].Equals(new command { }))
                    {
                        //Send command back 
                        SendCommandBack(ref newCommand);
                        //Console.WriteLine("REORDER BUFFER IS TOO FULL FOR THE NEW COMMAND");
                    }
                    //Check to see if there are true dependencies 
                    //Check that r1 and r2 from new command aren't changed by commands above
                    for (int a = 0; a < i; a++)
                    {
                        if (newCommand.dependencies.Contains(contenseOfReOrderBuffer[a].destination))
                        {
                            DebugLog($"{contenseOfReOrderBuffer[a].opCode} stopped {newCommand.opCode} due to dependency");
                            //send new Command back 
                            SendCommandBack(ref newCommand);
                            return;
                        }
                    }
                    //Check to see if there are true dependencies 
                    //Check all commands below aren't changed by this new destination
                    for (int x = SizeOfReOrderBuffer - 1; x > i; x--)
                    {
                        //Check to see if there are commands below (stop when we hit a new command)
                        if (contenseOfReOrderBuffer[x].Equals(new command { })) break;
                        if (contenseOfReOrderBuffer[x].dependencies.Contains(newCommand.destination))
                        {
                            DebugLog($"{newCommand.opCode} removed {contenseOfReOrderBuffer[x].opCode} due to dependency");
                            //We need to recalulate this command
                            SendCommandBack(ref newCommand);
                            contenseOfReOrderBuffer.Remove(contenseOfReOrderBuffer[x]);
                        }
                    }

                    DebugLog($"Put command in to reorder buffer {newCommand.opCode} because Last PC is {ShadowProgramCounter} and issued is {newCommand.PC}");
                    contenseOfReOrderBuffer.Insert(i, newCommand);
                    break;
                }
            }
        }
    }
    //We actually write back to memory (the end of the command)
    static public void Commit(ref command Command)
    {
        if (DetectDependency(ref Command) == true)
        {
            SendCommandBack(ref Command);
            return;
        }

        if (Command.destination.Contains("r") == true && DependencyTracker.Count != 0)
        {
            for (int i = 0; i < DependencyTracker.Count; i++)
            {
                if (DependencyTracker[i].registerName == Command.destination)
                {
                    if (DependencyTracker[i].cycleLastEditedIn < Command.cycleCalculatedIn) DependencyTracker.Remove(DependencyTracker[i]);
                    else
                    {
                        SendCommandBack(ref Command);
                        return;
                    }
                }
            }
        }
        if (Command.destination.Contains("r") == true)
        {
            DependencyTracker.Add(new dTracker
            {
                registerName = Command.destination,
                cycleLastEditedIn = Command.cycleCalculatedIn
            });
        }
        HistoryOutput = HistoryOutput + Command.opCode + " | ";
        //REGISTER COMMANDS
        if (Command.opCode == "LDC")
        {
            Memory.PutValueInRegister(Command.destination, Command.value1);
            DebugLogOutput($"Commited Load {Command.value1} to {Command.destination}");
        }
        else if (Command.opCode == "LD")
        {
            Memory.PutValueInRegister(Command.destination, Memory.GetValueFromRegister($"r{Memory.GetValueFromRegister(Command.valueString1)}"));
            DebugLogOutput($"Commited Load with offset so {Command.destination} has {Memory.GetValueFromRegister(Command.destination)}");
        }
        else if (Command.opCode == "STR")
        {
            //STR this.value this.value.indexedRegister
            //Confusing as valueString2 relates to r1 and valueString1 relates to r2
            string index = $"r{Memory.GetValueFromRegister(Command.valueString1)}";
            int valueToBeInserted = Memory.GetValueFromRegister(Command.valueString2);
            Memory.PutValueInRegister(index, valueToBeInserted);
            DebugLogOutput($"Commited store {valueToBeInserted} to {index}");
        }
        //BRANCH COMMANDS result:1 => take it || result:0 => Dont take it
        else if (Command.opCode == "BEQ" || Command.opCode == "BNE")
        {
            foreach(command cmd in predictions){
                if(cmd.assemblyCode == Command.assemblyCode) {
                    if(cmd.result == Command.result) BranchPrediction.correctGuesses++;
                    else BranchPrediction.incorrectGuesses++;
                    predictions.Remove(cmd);
                    break;
                }
            }
            DebugLogOutput($"Commited {Command.opCode} with {Command.result}");
            if (Command.result == 1)
            {
                BranchPrediction.PredictionResult(true);
                if (speculativeCommands.Count != 0)
                {
                    //Check to see if this is the branch predicted
                    if (speculativeCommands[0].assemblyCode == Command.assemblyCode)
                    {
                        BranchPrediction.BranchDebug($"Speculative branch is being check against the real excution");
                        if (speculativeCommands[0].result == Command.result)
                        {
                            //Send back correct result
                            /*
                            BranchPrediction.correctGuesses++;
                            */
                            BranchPrediction.BranchDebug($"Branch Correct and being committed with {speculativeCommands.Count}");
                            AddCommandsToRoBWhenSpecBranchIsCorrect();
                        }
                        else
                        {
                            //Send back incorrect result
                            /*
                            BranchPrediction.incorrectGuesses++;
                            */
                            BranchPrediction.BranchDebug($"Branch InCorrect and being destroyed");
                        }
                        CleanUpSpeculativeCommands();
                    }
                }
                //-1 Shadow counter because we add one to it at the end anyway
                ProgramCounter = Int32.Parse(Command.destination);
                ShadowProgramCounter = ProgramCounter - 1;
                DebugLogOutput($"Commited new pc {ProgramCounter}");
                //So we get rid off all commands in the reorder buffer as they're now redudent
                contenseOfReOrderBuffer = new List<command>(new command[SizeOfReOrderBuffer]);
                DependencyTracker = new List<dTracker>();
            }
            else BranchPrediction.PredictionResult(false);
        }
        else if (Command.opCode == "JUMP")
        {
            //-1 Shadow counter because we add one to it at the end anyway
            ProgramCounter = Int32.Parse(Command.destination);
            ShadowProgramCounter = ProgramCounter - 1;
            DebugLogOutput($"Commited new jump {ProgramCounter}");
            //So we get rid off all commands in the reorder buffer as they're now redudent
            contenseOfReOrderBuffer = new List<command>(new command[SizeOfReOrderBuffer]);
            DependencyTracker = new List<dTracker>();
        }
        //ARTHEMETRIC
        else if (Command.opCode == "ADDI" || Command.opCode == "ADD" || Command.opCode == "SUB" ||
                Command.opCode == "COMP" || Command.opCode == "MUL" || Command.opCode == "DIV")
        {
            Memory.PutValueInRegister(Command.destination, Command.result);
            DebugLogOutput($"Commited ALU {Command.result} to {Command.destination} OPCODE: {Command.opCode}");
        }
        //NOP
        else if (Command.opCode == "NOP")
        {
            DebugLogOutput($"Commited NOP");
            Console.WriteLine("STOP!");
            runProcessor = false;
        }
        else Console.WriteLine($"COMMIT GOT UNRECOGNISED OPCODE {Command.opCode}");
        //Increase LastProgramCounterExcuted because we have committed again 
        ShadowProgramCounter++;

        //Run to see if we can commit again!
        for (int i = 0; i < SizeOfReOrderBuffer; i++)
        {
            //Check to see if the rest of the buffer is empty
            if (contenseOfReOrderBuffer[i].Equals(new command { })) return;
            else if (contenseOfReOrderBuffer[i].PC == ShadowProgramCounter)
            {
                //DebugLogOutput($"Command in reorder buffer is now commitable {contenseOfReOrderBuffer[0].opCode}");
                command newCommand = contenseOfReOrderBuffer[i];
                PopFromReOrderBuffer();
                Commit(ref newCommand);
                return;
            }
        }
    }
    //Detect dependency returns true for dependency (spend back) and false for no dependency 
    static bool DetectDependency(ref command Command)
    {
        //int to count number of dependencies detected
        int dependencyDetected = 0;
        //If it has no dependencies then it's not gonna detect anything
        if (Command.dependencies == new List<string>()) return false;
        //Putting values into holders so that we dont cause index range exceptions
        string dependencyOne = "";
        if (Command.dependencies.Count > 0) dependencyOne = Command.dependencies[0];
        string dependencyTwo = "";
        if (Command.dependencies.Count > 1) dependencyTwo = Command.dependencies[1].Remove(0, 1);
        foreach (dTracker dTrack in DependencyTracker)
        {
            if (dTrack.registerName.Contains(dependencyOne) == true || dTrack.registerName.Contains(dependencyTwo) == true)
            {
                //Console.WriteLine($"Actual check on {Command.opCode} with {Command.cycleCalculatedIn} comp to dTrack{dTrack.cycleLastEditedIn}");
                if (!(Command.cycleCalculatedIn > dTrack.cycleLastEditedIn))
                {
                    dependencyDetected++;
                }
            }
        }
        if (dependencyDetected == 0) return false;
        else return true;
    }
    //Detecting a true dependency we send it back to be recalucated
    static void SendCommandBack(ref command Command)
    {
        numberOfCommandsSentBack++;
        ExcutionUnits.AssignToExcutionUnit(Command);
    }
    static void PopFromReOrderBuffer()
    {
        if (contenseOfReOrderBuffer[0].Equals(new command { })) return;

        for (int i = 0; i < SizeOfReOrderBuffer - 1; i++)
        {
            contenseOfReOrderBuffer[i] = contenseOfReOrderBuffer[i + 1];
        }
        contenseOfReOrderBuffer[SizeOfReOrderBuffer - 1] = new command { };
    }
    //called once speculative branch is checked
    //Removes all commands until it finds a branch command
    static void CleanUpSpeculativeCommands()
    {
        foreach (command cmd in speculativeCommands)
        {
            if (cmd.opCode == "BEQ" || cmd.opCode == "BNE") return;
            speculativeCommands.Remove(cmd);
        }
    }
    //Called when speculative branch is correct so we need to add the commands to the RoB
    static void AddCommandsToRoBWhenSpecBranchIsCorrect()
    {
        //We know that the specBranch branch command is the same as the real one so we delete it!
        speculativeCommands.RemoveAt(0);
        foreach (command cmd in speculativeCommands)
        {
            if (cmd.opCode == "BEQ" || cmd.opCode == "BNE") return;
            BranchPrediction.BranchDebug($"Command {cmd.opCode} is being added to RoB by branch predictor");
            command acceptedSpecBranch = cmd;
            acceptedSpecBranch.specBranch = 0;
            addCommand(acceptedSpecBranch);
        }
    }
    static public void SendPrediction(command prediction){
        predictions.Add(prediction);
    }
    #region Debugging
    static private void DebugLog(string debugPrint)
    {
        if (ReOrderBufferDebug == true) Console.WriteLine(debugPrint);
    }
    static private void DebugLogOutput(string debugPrint)
    {
        if (ReOrderBufferDebugOutput == true) Console.WriteLine(debugPrint);
    }
    static public void PrintOutReOrderBufferHistory()
    {
        Console.WriteLine("---------------- ReOrder Buffer History ----------------");
        //Console.Write("--- ReOrder Buffer Input History");
        //Console.WriteLine($"{HistoryInput}");
        Console.Write("--- ReOrder Buffer Output History");
        Console.WriteLine($"{HistoryOutput}");
        Console.WriteLine($"Number of commands sent back {numberOfCommandsSentBack}");
    }
    #endregion
}