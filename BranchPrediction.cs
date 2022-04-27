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
    //Type 3 (static: if backward take)
    //Type 4 (one state)
    //Type 5 (two state)
    static int typeOfBranchPrediction = 1;
    public static bool oneState = true;
    //0 strongly not take //1 weakly not taken //2 weakly taken //3 strongly taken
    public static int twoState = 0;
    public static int correctGuesses = 0;
    public static int incorrectGuesses = 0;
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
        else if (typeOfBranchPrediction == 2)
        {
            //Type 2 (static: if forward take)
            //if destination is bigger then it's forward
            if (Int32.Parse(branchCommand.destination) > ProgramCounter)
            {
                branchCommand.result = 1;
            }
            else branchCommand.result = 0;
        }
        else if (typeOfBranchPrediction == 3)
        {
            //Type 3 (stastic: if backward take)
            //if destination is smaller then it's backward
            if (Int32.Parse(branchCommand.destination) < ProgramCounter)
            {
                branchCommand.result = 1;
            }
            else branchCommand.result = 0;
        }
        else if (typeOfBranchPrediction == 4)
        {
            //Type 4 (one state)
            if (oneState == true)
            {
                branchCommand.result = 1;
            }
            else branchCommand.result = 0;
        }
        else if (typeOfBranchPrediction == 5)
        {
            //Type 5 (two state)
            if (twoState == 0 || twoState == 1)
            {
                branchCommand.result = 0;
            }
            else branchCommand.result = 1;
        }
        ReOrderBuffer.addCommand(branchCommand);
        //Get pipe to fetch these commands when they are'ny busy with none speculative commands
        if (branchCommand.result == 1)
        {
            Pipe.LoadSpeculativeCommands(Int32.Parse(branchCommand.destination));
        }
    }
    public static void PredictionResult(bool taken)
    {
        if (typeOfBranchPrediction == 4)
        {
            if (taken == true)
            {
                oneState = true;
            }
            else
            {
                oneState = false;
            }
        }
        else if (typeOfBranchPrediction == 5)
        {
            if (taken == true)
            {
                if (twoState != 3) twoState++;
            }
            else
            {
                if (twoState != 0) twoState--;
            }
        }
    }
    public static void BranchDebug(string debug)
    {
        if (BranchPredictorDebug == true) Console.WriteLine(debug);
    }
}