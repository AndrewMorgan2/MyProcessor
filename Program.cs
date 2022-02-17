using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyProcessor
{
    struct pipeData
    {
        public int Name;
        public string CommandHistory;
        public string ActiveCommand;
        public int instructionCycles;
    }

    class Program
    {
        static int NumberOfPipes = 3;
        static int waitingCycles = 0;
        static void Main(string[] args)
        {
            //Instatiate Pipes
            pipeData[] pipes = new pipeData[NumberOfPipes];

            for(int i = 0; i < NumberOfPipes; i++)
            {
                pipes[i].Name = i;
                pipes[i].ActiveCommand = null;
                pipes[i].CommandHistory = null;
                pipes[i].instructionCycles = 0;
            }
            //Read in commands into a useable arrray
            string[] instructionList = System.IO.File.ReadAllLines(@"./instructionSet.txt");
            int NextInstruction = 0;
            System.Console.WriteLine($"Now lets run our processor with {instructionList.Length} commands");

            //Assign instruction to pipe 
            for(int i = 0; NumberOfPipes > i; i++){
                //Do we still have instruction to excute
                if(instructionList.Length > NextInstruction)
                {
                    if(pipes[i].ActiveCommand == null){
                        Console.Write($"Pipe {pipes[i].Name} has been given: ");
                        PipeReplaceCommand(null, String.Format($"Fetch {instructionList[NextInstruction]}"), ref pipes[i]);
                        //This should be parallised
                        processCommands(instructionList[NextInstruction], ref pipes[i]);

                        //What are all the other pipes up to 
                        for(int b = 0; NumberOfPipes > b; b++){
                            //we know what pipes[i] is doing
                            if(pipes[b].Name != pipes[i].Name){
                                if(pipes[b].ActiveCommand == null){
                                    //Console.WriteLine($"pipe {pipes[b].Name} is going to wait");
                                    PipeReplaceCommand("Waiting", null, ref pipes[b]);
                                    //Add one to total waiting cycles
                                    waitingCycles++;
                                }
                            }
                        }  
                        NextInstruction++;
                    }
                }
            }

            Console.WriteLine("----------------  Pipes history    ----------------");
            int actionsTaken = 0;
            for(int i = 0; NumberOfPipes > i; i++){
                if(pipes[i].CommandHistory == null) System.Console.WriteLine($" Pipe {pipes[i].Name} wasn't used");
                else{
                    System.Console.Write($" --- Pipe {pipes[i].Name}'s History");
                    System.Console.WriteLine(pipes[i].CommandHistory);
                    actionsTaken = actionsTaken + pipes[i].instructionCycles;
                }
            }
            //End of program
            Console.WriteLine($"Actions taken {actionsTaken}");
            Console.WriteLine($"Actions taken {waitingCycles}");
        }

        static void processCommands(string command, ref pipeData pipe){
            //Update pipeline
            PipeReplaceCommand(command, "Decode", ref pipe);
            //First part is the opcode
            string opCode = getNextPartFromText(command);
            excutionUnitManager(opCode, command, ref pipe);
        }

        #region Excution Units
        /*Excution Units*/
        static void excutionUnitManager(string opCode, string command, ref pipeData pipe){
            //Update pipeline
            PipeReplaceCommand("Decode", "Excute", ref pipe);

            //Here's where we decide what to actually do
            arthmetricOp(opCode, command);

            //Update pipeline
            PipeReplaceCommand("Excute", null, ref pipe);
        }
        static void arthmetricOp(string opcode, string command){
            command = command.Remove(0,4);
            //We need the next part of the instruction
            //r1 can be an int or a direction to a register
            string r1 = getNextPartFromText(command);
            //r2 can be an int or a direction to a register
            command = command.Remove(0, r1.Length);
            string r2 = getNextPartFromText(command);
            if(opcode == "ADD"){
                addEU(command, r1, r2);
            }
            if(opcode == "SUB"){
                subEU(command, r1, r2);
            }
        }
        static void addEU(string command, string r1, string r2){
            //ADD r1 r2 
            int result = Int32.Parse(r1) + Int32.Parse(r2);
            System.Console.WriteLine($"Adding {r1} to {r2}: Result {result}");
        }
        static void subEU(string command, string r1, string r2){
            //SUB r1 r2 
            int result = Int32.Parse(r1) - Int32.Parse(r2);
            System.Console.WriteLine($"Subtracting {r1} to {r2}: Result {result}");
        }
        #endregion

        #region Pipe Functions
        static void PipeReplaceCommand(string oldCommand, string newCommand, ref pipeData pipe){
            if(oldCommand != null){
                //Add old Command to history 
                pipe.CommandHistory = pipe.CommandHistory + " | " + oldCommand;
            }
            //Replace new active command
            pipe.ActiveCommand = newCommand;
            if(newCommand != null){
                pipe.instructionCycles++;
            }
        }   
        #endregion

        /*Functions that are misc*/
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
    }
}
