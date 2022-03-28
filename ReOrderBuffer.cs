using System;
using System.Collections.Generic;
using static MyProcessor.Processor;
using command = MyProcessor.command;
static class ReOrderBuffer
{
    #region static public Vars
    static public List<command> contenseOfReOrderBuffer;
    static public int LastExcutionOrder;
    static private string HistoryInput,HistoryOutput;
    #endregion
    //Called at start to make the RoB
    static public void makeReOrderBuffer()
    {
        contenseOfReOrderBuffer = new List<command>(new command[SizeOfReOrderBuffer]);
        LastExcutionOrder = 0;
        HistoryOutput = "";
        HistoryInput = "";
    }
    //We are going to check to see if we should commit or we should keep in the reorder buffer
    static public void addCommand(command newCommand)
    {
        //DebugLog($"Recieved command {newCommand.opCode}");
        HistoryInput = HistoryInput + newCommand.opCode + " | ";
        if (newCommand.issuedOrder == LastExcutionOrder)
        {
            //Send to commit unit
            Commit(ref newCommand);
        }
        else
        {
            //Add to list in correct place 
            for (int i = 0; i < SizeOfReOrderBuffer; i++)
            {
                //if empty place or issued earlier
                if (contenseOfReOrderBuffer[i].Equals(new command { }) || newCommand.issuedOrder < contenseOfReOrderBuffer[i].issuedOrder)
                {
                    //Check to see if the buffer is full
                    if (!contenseOfReOrderBuffer[SizeOfReOrderBuffer - 1].Equals(new command { }))
                    {
                        Console.WriteLine("REORDER BUFFER IS TOO FULL FOR THE NEW COMMAND");
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

                    DebugLog($"Put command in to reorder buffer {newCommand.opCode} because Last EX is {LastExcutionOrder} and issued is {newCommand.issuedOrder}");
                    contenseOfReOrderBuffer.Insert(i, newCommand);
                    break;
                }
            }
        }
    }
    //We actually write back to memory (the end of the command)
    static public void Commit(ref command Command)
    {
        HistoryOutput = HistoryOutput + Command.opCode + " | ";
        //REGISTER COMMANDS
        if (Command.opCode == "LDC")
        {
            Memory.PutValueInRegister(Command.destination, Command.value1);
            DebugLogOutput($"Commited Load {Command.value1} to {Command.destination}");
        }
        //BRANCH COMMANDS result:1 => take it || result:) => Dont take it
        else if (Command.opCode == "BEQ" || Command.opCode == "BNE")
        {
            if (Command.result == 1)
            {
                ProgramCounter = Command.value1;
                DebugLogOutput($"Commited new pc {ProgramCounter}");
            }
        }
        //ARTHEMETRIC
        else if (Command.opCode == "ADDI" || Command.opCode == "ADD" || Command.opCode == "SUB" ||
                Command.opCode == "COMP" || Command.opCode == "MUL" || Command.opCode == "DIV")
        {
            Memory.PutValueInRegister(Command.destination, Command.result);
            DebugLogOutput($"Commited ALU {Command.result} to {Command.destination}");
        }
        else Console.WriteLine($"COMMIT GOT UNRECOGNISED OPCODE {Command.opCode}");
        //Increase LastProgramCounterExcuted because we have committed again 
        LastExcutionOrder++;

        //Check to see if buffer is empty
        if (contenseOfReOrderBuffer[0].Equals(new command { })) return;
        //Run to see if we can commit again!
        if (contenseOfReOrderBuffer[0].issuedOrder == LastExcutionOrder)
        {
            DebugLogOutput($"Command in reorder buffer is now commitable {contenseOfReOrderBuffer[0].opCode}");
            command newCommand = contenseOfReOrderBuffer[0];
            PopFromReOrderBuffer();
            Commit(ref newCommand);
        }
    }
    //Detecting a true dependency we send it back to be recalucated
    static void SendCommandBack(ref command Command)
    {
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
        Console.Write("--- ReOrder Buffer Input History");
        Console.WriteLine($"{HistoryInput}");
        Console.Write("--- ReOrder Buffer Output History");
        Console.WriteLine($"{HistoryOutput}");
    }
    #endregion
}