using System;
using System.Collections.Generic;
using static MyProcessor.Processor;
using command = MyProcessor.command;

public class Pipe
{
    #region Data Structs and pipes
    public struct pipeData
    {
        public int Name;
        public string assemblyCode;
        //Used to debug at the end
        public string CommandHistory;
        //How we know what the pipe is doing
        public string ActiveCommand;
        //What we decode
        public string CurrentInstruction;
        //Opcode from decode
        public string Opcode;
        //Where the operation will end up
        public string Destination;
        public string[] valueRegisters;
        public int instructionCycles;
        //Used to implement division/multiple/load/store taking more than one cycle
        public int numCyclesBusyFor;
        public bool busy;
        public int PC;
        public int specBranch;
    }
    static public pipeData[] pipes = new pipeData[NumberOfPipes];
    static public List<command> sentBackCommands = new List<command>();
    static public List<string> speculativeInstructions = new List<string>();
    static public int speculativeProgramCounter = 0;
    #endregion
    //Called at the start to make the pipes
    static public void makePipes()
    {
        for (int i = 0; i < NumberOfPipes; i++)
        {
            pipes[i].Name = i;
            pipes[i].ActiveCommand = null;
            pipes[i].CommandHistory = null;
            pipes[i].instructionCycles = 0;
            pipes[i].numCyclesBusyFor = 0;
            //We only have two registers per pipe
            pipes[i].valueRegisters = new string[2];
        }
    }
    //We add to pipe history and replace the current command the pipe has 
    static public void PipeReplaceCommand(string oldCommand, string newCommand, int pipeName)
    {
        //aConsole.WriteLine($"Pipe:{pipe.Name} has gone from {oldCommand} to {newCommand}");
        if (oldCommand != null)
        {
            string commandToBeAdded = "";
            if (Pipe.pipes[pipeName].ActiveCommand == null || Pipe.pipes[pipeName].ActiveCommand == "Waiting")
            {
                commandToBeAdded = "WT";
            }
            else if (Pipe.pipes[pipeName].ActiveCommand.Substring(0, 5) == "Fetch")
            {
                commandToBeAdded = "IF";
            }
            else if (Pipe.pipes[pipeName].ActiveCommand == "Decode")
            {
                commandToBeAdded = "DE";
            }
            else if (Pipe.pipes[pipeName].ActiveCommand == "Excute")
            {
                commandToBeAdded = "EX";
            }
            else
            {
                Console.WriteLine($"Pipe {Pipe.pipes[pipeName].Name} has a unrecognised active command");
            }
            //Add old Command to history 
            Pipe.pipes[pipeName].CommandHistory = Pipe.pipes[pipeName].CommandHistory + " | " + commandToBeAdded;
        }
        //Replace new active command
        Pipe.pipes[pipeName].ActiveCommand = newCommand;
        if (newCommand != null)
        {
            Pipe.pipes[pipeName].instructionCycles++;
        }
    }
    //See if any pipes are not busy, give free pipes a command (calls PipeReplaceCommand)
    static public void PipeAssignment(string[] instructionList, ref int ProgramCounter)
    {
        //Assign instruction to pipe 
        for (int i = 0; NumberOfPipes > i; i++)
        {
            if (ProgramCounter < instructionList.Length)
            {
                //Check to see if a pipe is empty or if it's excuting something fast
                if (pipes[i].ActiveCommand == null || pipes[i].ActiveCommand == "Waiting" || (pipes[i].ActiveCommand == "Excute" && pipes[i].busy == false))
                {
                    //Make this a none speculative branch 
                    Pipe.pipes[i].specBranch = 0;
                    //Check to see if any commands have been sent back before taking a new command
                    if (sentBackCommands.Count == 0)
                    {
                        //If we see a branch command then send it to the branch predictor aswell
                        if (instructionList[ProgramCounter].Substring(0, 3) == "BEQ" || instructionList[ProgramCounter].Substring(0, 3) == "BNE")
                        {
                            BranchPrediction.SendBranchToPrediction(instructionList[ProgramCounter]);
                        }
                        //Debug assignment
                        if (PipeAssignmentDebug == true) Console.WriteLine($"Pipe {pipes[i].Name} has been given: {instructionList[ProgramCounter]}");

                        PipeReplaceCommand(pipes[i].ActiveCommand, String.Format($"Fetch {instructionList[ProgramCounter]}"), pipes[i].Name);
                        Pipe.pipes[i].assemblyCode = instructionList[ProgramCounter];
                        Pipe.pipes[i].PC = ProgramCounter;
                        ProgramCounter++;
                    }
                    else
                    {
                        //Debug assignment
                        if (PipeAssignmentDebug == true) Console.WriteLine($"Pipe {pipes[i].Name} has been given: {sentBackCommands[0].assemblyCode}");

                        PipeReplaceCommand(pipes[i].ActiveCommand, String.Format($"Fetch {sentBackCommands[0].assemblyCode}"), pipes[i].Name);
                        Pipe.pipes[i].PC = sentBackCommands[0].PC;
                    }
                    ExcutionOrder++;

                    //What are all the other pipes up to 
                    for (int b = 0; NumberOfPipes > b; b++)
                    {
                        //we know what pipes[i] is doing
                        //We've also already updated all pipes that are smaller than the current i 
                        if (pipes[b].Name != pipes[i].Name && i < b)
                        {
                            //Lets update the instructions of the pipes
                            UpdatePipe(Pipe.pipes[b].Name);
                        }
                    }
                    //We don't want all empty pipes to be assigned the same task
                    break;
                }
                //If we have full pipes then we just want them to update
                else UpdatePipe(Pipe.pipes[i].Name);
            }
            //Check to see if we have speculative commands to fetch
            else if (speculativeInstructions.Count < 0)
            {
                //Check to see if a pipe is empty or if it's excuting something fast
                if (pipes[i].ActiveCommand == null || pipes[i].ActiveCommand == "Waiting" || (pipes[i].ActiveCommand == "Excute" && pipes[i].busy == false))
                {
                    //Make this a speculative branch 
                    Pipe.pipes[i].specBranch = 1;
                    //Debug assignment
                    if (PipeAssignmentDebug == true) Console.WriteLine($"Pipe {pipes[i].Name} has been given: {speculativeInstructions[0]}");

                    PipeReplaceCommand(pipes[i].ActiveCommand, String.Format($"Fetch {speculativeInstructions[0]}"), pipes[i].Name);
                    Pipe.pipes[i].assemblyCode = speculativeInstructions[0];
                    Pipe.pipes[i].PC = speculativeProgramCounter;
                    Pipe.pipes[i].specBranch = 1;
                    speculativeProgramCounter++;
                    speculativeInstructions.Remove(speculativeInstructions[0]);
                    //What are all the other pipes up to 
                    for (int b = 0; NumberOfPipes > b; b++)
                    {
                        //we know what pipes[i] is doing
                        //We've also already updated all pipes that are smaller than the current i 
                        if (pipes[b].Name != pipes[i].Name && i < b)
                        {
                            //Lets update the instructions of the pipes
                            UpdatePipe(Pipe.pipes[b].Name);
                        }
                    }
                }
            }
            else
            {
                UpdatePipe(Pipe.pipes[i].Name);
            }
        }
        Excute();
    }
    //Called once per cycle to progress the state of the pipe 
    static void UpdatePipe(int pipeName)
    {
        if (Pipe.pipes[pipeName].ActiveCommand == null || Pipe.pipes[pipeName].ActiveCommand == "Waiting")
        {
            //Console.WriteLine($"pipe {pipe.Name} is going to wait");
            PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Waiting", pipeName);
            //Console.WriteLine($"Pipe {pipes[b].Name} is waiting");
        }
        else if (Pipe.pipes[pipeName].ActiveCommand.Substring(0, 5) == "Fetch")
        {
            PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Decode", pipeName);
        }
        else if (Pipe.pipes[pipeName].ActiveCommand == "Decode")
        {
            //Console.WriteLine($"{Pipe.pipes[pipeName].CommandHistory}");
            PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Excute", pipeName);
        }
        else if (Pipe.pipes[pipeName].ActiveCommand == "Excute")
        {
            //Check to see if the pipe is still excuting 
            if (Pipe.pipes[pipeName].numCyclesBusyFor == 0 && Pipe.pipes[pipeName].busy == false)
            {
                PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Waiting", pipeName);
            }
            else
            {
                PipeReplaceCommand(Pipe.pipes[pipeName].ActiveCommand, "Excute", pipeName);
            }
        }
        else
        {
            Console.WriteLine($"Pipe {Pipe.pipes[pipeName].Name} has a unrecognised active command");
        }
    }
    //Call processCommands for all pipes
    static void Excute()
    {
        //Excute pipe instructions
        for (int i = 0; NumberOfPipes > i; i++)
        {
            //Console.WriteLine($"{pipes[i].ActiveCommand}");
            processCommands(pipes[i].ActiveCommand, pipes[i].Name);
        }
    }
    //Actually does what the pipes active command is 
    static void processCommands(string command, int pipeName)
    {
        //Console.WriteLine($"Command: {command} in pipe: {pipeName}");
        //if the pipe is sleeping then we just leave
        if (command == "Waiting")
        {
            //Wait
            //Add one to total waiting cycles
            waitingCycles++;
        }
        else if (command.Substring(0, 5) == "Fetch")
        {
            //We fetch 
            Pipe.pipes[pipeName].CurrentInstruction = Pipe.pipes[pipeName].ActiveCommand.Remove(0, 5);
        }
        else if (command == "Decode")
        {
            //We decode 
            Decode(pipeName);
        }
        else if (command == "Excute")
        {
            //We excute
            ExcutionUnits.excutionUnitManager(pipeName);
        }
        return;
    }
    //Takes the string and splits it into useful information then stores it in pipeData
    static void Decode(int pipeName)
    {
        //We don't want the fetch so we get rid of that straight away 
        string currentInstruction = Pipe.pipes[pipeName].CurrentInstruction;
        string opCode = getNextPartFromText(currentInstruction);
        string destination = "";
        //Set defult pipe values 
        pipes[pipeName].valueRegisters = new string[2];

        /*DECODE TENDS TO FAIL WHEN PROGRAMS ARE WRITTEN POORLY SO BEFORE 
        CHANGING THIS CODE MAKE SURE ALL COMMANDS ARE CORRECTLY FORMATTED */

        //We dont want to keep decoding if we see NOP
        if (opCode != "NOP")
        {
            currentInstruction = currentInstruction.Remove(0, opCode.Length + 1);
            destination = getNextPartFromText(currentInstruction);
            currentInstruction = currentInstruction.Remove(0, destination.Length + 1);
            string r2;
            //Change LD to LDC
            if (opCode == "LD")
            {
                r2 = getNextPartFromText(currentInstruction);
                currentInstruction = currentInstruction.Remove(0, r2.Length + 1);
                Pipe.pipes[pipeName].valueRegisters[0] = currentInstruction;
                opCode = "LDC";
            }
            else if (opCode == "JUMP")
            {
                Pipe.pipes[pipeName].valueRegisters[0] = destination;
            }
            //Decode Happens here
            else
            {
                r2 = getNextPartFromText(currentInstruction);
                Pipe.pipes[pipeName].valueRegisters[0] = r2;
                if (currentInstruction.Length > r2.Length + 1)
                {
                    currentInstruction = currentInstruction.Remove(0, r2.Length + 1);
                    string r3 = currentInstruction;
                    Pipe.pipes[pipeName].valueRegisters[1] = r3;
                }
            }
        }
        //Give opCode and destination
        Pipe.pipes[pipeName].Opcode = opCode;
        Pipe.pipes[pipeName].Destination = destination;
    }
    //Function to load speculative commands into the list of speculative commands
    static public void LoadSpeculativeCommands(int ProgramCounterOfSpeculativeBranch)
    {
        speculativeProgramCounter = ProgramCounterOfSpeculativeBranch;
        //Load all instructions between the new PC and the end
        for (int i = ProgramCounterOfSpeculativeBranch; i < instructionList.Length; i++)
        {
            if ("BEQ" == getNextPartFromText(instructionList[i])) return;
            if ("BNE" == getNextPartFromText(instructionList[i])) return;
            if (speculativeInstructions.Contains(instructionList[i]) == false)
            {
                speculativeInstructions.Add(instructionList[i]);
                BranchPrediction.BranchDebug($"Added {instructionList[i]} to spec instructions in pipes");
            }
        }
    }
}