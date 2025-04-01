using System;
using System.Collections.Generic;

namespace COSC365Assembler
{
    // Example instruction as provided in the assignment
    public class Dup : IInstruction 
    {
        private readonly int _offset;
        
        public Dup(int offset) 
        {
            _offset = offset & ~3; // Ensure it's a multiple of 4
        }
        
        public int Encode() 
        {
            return (0b1100 << 28) | _offset;
        }
    }
    
    // Add classes for each instruction type here
    
    // Implement a basic push instruction
    public class Push : IInstruction
    {
        private readonly int _value;
        
        public Push(int value)
        {
            _value = value;
        }
        
        public int Encode()
        {
            // Push opcode is 15 (1111 in binary)
            // Keep only the 28 least significant bits of the value
            return (15 << 28) | (_value & 0x0FFFFFFF);
        }
    }
    
    // Implement NOP for padding
    public class Nop : IInstruction
    {
        public int Encode()
        {
            // Opcode 0, subopcode 2 (0000 0010)
            return (0 << 28) | (2 << 24);
        }
    }
    
    // Miscellaneous Instructions (opcode=0)
    // Exit, Swap, Input, StInput, Debug
    
    // Pop Instructions (opcode=1)
    
    // Binary Arithmetic Instructions (opcode=2)
    // Add, Sub, Mul, Div, Rem, And, Or, Xor, Lsl, Lsr, Asr
    
    // Unary Arithmetic Instructions (opcode=3)
    // Neg, Not
    
    // String Print Instructions (opcode=4)
    
    // Call Instructions (opcode=5)
    
    // Return Instructions (opcode=6)
    
    // Unconditional Goto Instructions (opcode=7)
    
    // Binary If Instructions (opcode=8)
    
    // Unary If Instructions (opcode=9)
    
    // Print Instructions (opcode=13)
    
    // Dump Instructions (opcode=14)
    
    // Factory method to create instructions (helpful utility)
    public static class InstructionFactory
    {
        public static IInstruction CreateInstruction(string opcode, string[] operands, Dictionary<string, int> labels, int currentPc)
        {
            switch (opcode.ToLower())
            {
                case "dup":
                    int offset = operands.Length > 0 ? ParseOffset(operands[0]) : 0;
                    return new Dup(offset);
                
                case "push":
                    int value = 0;
                    if (operands.Length > 0)
                    {
                        // Handle decimal or hex values
                        if (operands[0].StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            value = Convert.ToInt32(operands[0].Substring(2), 16);
                        }
                        else
                        {
                            value = int.Parse(operands[0]);
                        }
                    }
                    return new Push(value);
                
                case "nop":
                    return new Nop();
                    
                // Add cases for all other instructions
                
                default:
                    throw new Exception($"Unknown instruction: {opcode}");
            }
        }
        
        private static int ParseOffset(string offsetStr)
        {
            // Parse decimal or hex offset
            if (offsetStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(offsetStr.Substring(2), 16);
            }
            return int.Parse(offsetStr);
        }
        
        // Add helper methods for expanding pseudo-instructions like stpush
        public static List<IInstruction> ExpandStPush(string stringLiteral)
        {
            // Logic to expand stpush into multiple push instructions
            return new List<IInstruction>();
        }
    }
}