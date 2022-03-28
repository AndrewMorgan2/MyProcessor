using System;
using static MyProcessor.Processor;

public class Memory
{
    #region Data Structs and RegFile
    public struct cache
    {
        public int[] memory;
    }
    public struct registerFile
    {
        public cache[] caches;
    }
    public static registerFile Regfile = new registerFile();
    #endregion
    //Called at the start of the program to generate the memory 
    static public void makeMemory()
    {
        //Instatiate cache
        registerFile regFile = new registerFile
        {
            caches = new cache[NumberOfCache]
        };

        for (int i = 0; i < NumberOfCache; i++)
        {
            cache cacheToBeAddedToRegister = new cache
            {
                memory = new int[SizeOfCache]
            };
            regFile.caches[i] = cacheToBeAddedToRegister;
        }
        Regfile = regFile;
    }
    //Called by commit and is the write back functionality 
    static public void PutValueInRegister(string registerIndexString, int value)
    {
        if (registerIndexString.Contains('r') == false) Console.WriteLine("Register passed without register flag!");
        //Remove the r
        int registerIndex = RemoveR(registerIndexString);
        //Find which cache do we need to hit
        int cacheNumber = 0;
        if (registerIndex > SizeOfCache)
        {
            registerIndex = registerIndex - SizeOfCache;
            cacheNumber++;
        }
        //Check if we have that register
        if (cacheNumber > NumberOfCache)
        {
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
    //Called by decode to get values ready for excution unit
    static public int GetValueFromRegister(string registerIndexString)
    {
        if (registerIndexString.Contains('r') == false)
        {
            Console.WriteLine("Register passed without register flag! --- Cache miss");
            cacheMisses++;
            return 0;
        }
        //Remove the r
        int registerIndex = RemoveR(registerIndexString);
        //Find which cache do we need to hit
        int cacheNumber = 0;
        if (registerIndex > SizeOfCache)
        {
            registerIndex = registerIndex - SizeOfCache;
            cacheNumber++;
        }
        return Regfile.caches[cacheNumber].memory[registerIndex];
    }
    static public int GetValueFromRegisterWithOffset(string registerIndexString, string offset)
    {
        if (registerIndexString.Contains('r') == false)
        {
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
        if (registerIndex > SizeOfCache)
        {
            registerIndex = registerIndex - SizeOfCache;
            cacheNumber++;
        }
        return Regfile.caches[cacheNumber].memory[registerIndex];
    }
    static int RemoveR(string r1)
    {
        //Remove the r
        int rPos = 0;
        for (int i = 0; i < r1.Length; i++)
        {
            rPos++;
            if (r1[i] == 'r') break;
        }
        return Int32.Parse(r1.Remove(0, rPos));
    }
}