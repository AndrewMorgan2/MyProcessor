using System;
using System.Collections.Generic;
using static MyProcessor.Processor;
using command = MyProcessor.command;
static class BranchPrediction
{
    //Type of branch predictor determines how we will assign a result
    //Type 0 (fixed: take)
    //Type 1 (fixed: dont take)
    //Type 2 (static: if forward take)
    //Type 3 (stastic: if backward take)
    //Type 4 (one state)
    //Type 5 (two state)
    static int typeOfBranchPrediction = 1;
    public static void SendBranchToPrediction(string instruction)
    {
        string assemblyCode = instruction;
        string opCodeBranch = getNextPartFromText(instruction);
        instruction = instruction.Remove(0, opCodeBranch.Length + 1);
        string destination = getNextPartFromText(instruction);
        //We dont need to track dependencies as we will only commit this when we have the real version in and it's result is the same as ours
        //We also dont need to track stuff like pc 
        //This command is more a storage for result 
        command branchCommand = new command
        {
            assemblyCode = assemblyCode,
            opCode = opCodeBranch,
            destination = destination,
            dependencies = new List<string>(),
            PC = 0,
            cycleCalculatedIn = 0,
            specBranch = 1,
            result = 0
        };
        branchCommand.specBranch = 1;
        if (typeOfBranchPrediction == 0)
        {
            //Fixed Take
            branchCommand.result = 0;
        }
        else if (typeOfBranchPrediction == 1)
        {
            //Fixed Not Take
            branchCommand.result = 1;
        }
        ReOrderBuffer.addCommand(branchCommand);
        //Get pipe to fetch these commands when they are'ny busy with none speculative commands
        if (branchCommand.result == 1)
        {
            Pipe.LoadSpeculativeCommands(Int32.Parse(branchCommand.destination));
        }
    }
    public static void BranchDebug(string debug)
    {
        if (BranchPredictorDebug == true) Console.WriteLine(debug);
    }
}