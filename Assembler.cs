using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace COSC365Assembler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dotnet run <input.asm> <output.v>");
                return;
            }
            
            string inputFile = args[0];
            string outputFile = args[1];
            
            Dictionary<string, int> labels = new Dictionary<string, int>();
            List<IInstruction> instructions = new List<IInstruction>();
            
            try
            {
                // Pass 1: Record labels
                FirstPass(inputFile, labels);
                
                // Pass 2: Encode instructions
                SecondPass(inputFile, labels, instructions);
                
                // Write output
                WriteOutput(outputFile, instructions);
                
                Console.WriteLine($"Successfully assembled {instructions.Count} instructions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        private static void FirstPass(string inputFile, Dictionary<string, int> labels)
        {
            string[] lines = File.ReadAllLines(inputFile);
            int pc = 0;
            
            foreach (string lineRaw in lines)
            {
                string line = RemoveComments(lineRaw.Trim());
                
                if (string.IsNullOrEmpty(line))
                    continue;
                
                if (line.EndsWith(":"))
                {
                    string label = line.Substring(0, line.Length - 1);
                    labels[label] = pc;
                    Console.WriteLine($"Label: {label} at address {pc}");
                }
                else
                {
                    // For now, we'll just handle 'push' as our test instruction
                    pc += 4; // All instructions are 4 bytes
                }
            }
        }
                
        private static void SecondPass(string inputFile, Dictionary<string, int> labels, List<IInstruction> instructions)
        {
            string[] lines = File.ReadAllLines(inputFile);
            int pc = 0;
            
            foreach (string lineRaw in lines)
            {
                string line = RemoveComments(lineRaw.Trim());
                
                if (string.IsNullOrEmpty(line) || line.EndsWith(":"))
                    continue;
                
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length > 0)
                {
                    string opcode = parts[0].ToLower();
                    string[] operands = parts.Length > 1 ? parts[1..] : new string[0];
                    
                    try 
                    {
                        IInstruction instruction = InstructionFactory.CreateInstruction(opcode, operands, labels, pc);
                        instructions.Add(instruction);
                        Console.WriteLine($"Added {opcode} instruction at address {pc}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: {ex.Message} (skipping instruction at address {pc})");
                    }
                    
                    pc += 4;
                }
            }
        }
        
        private static void WriteOutput(string outputFile, List<IInstruction> instructions)
        {
            if (instructions.Count == 0)
            {
                Console.WriteLine("Error: No instructions to assemble.");
                return;
            }
            
            using (BinaryWriter writer = new BinaryWriter(File.Open(outputFile, FileMode.Create)))
            {
                // Write magic header
                writer.Write(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
                
                // Write encoded instructions
                foreach (var inst in instructions)
                {
                    int encoded = inst.Encode();
                    writer.Write(encoded);
                    Console.WriteLine($"Wrote instruction: 0x{encoded:X8}");
                }
                
                // Pad with NOP instructions if needed
                int paddingNeeded = (4 - (instructions.Count % 4)) % 4;
                if (paddingNeeded > 0)
                {
                    Console.WriteLine($"Adding {paddingNeeded} NOP instructions for padding");
                    var nop = new Nop();
                    for (int i = 0; i < paddingNeeded; i++)
                    {
                        writer.Write(nop.Encode());
                    }
                }
            }
        }
        
        private static string RemoveComments(string line)
        {
            int commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                return line.Substring(0, commentIndex).Trim();
            }
            return line;
        }
    }
}