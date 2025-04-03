using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualMachineAssembler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: assembler <input.asm> <output.v>");
                return;
            }

            string inputFile = args[0];
            string outputFile = args[1];

            try
            {
                var assembler = new Assembler();
                assembler.Assemble(inputFile, outputFile);
                Console.WriteLine($"Successfully assembled {inputFile} to {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    class Assembler
    {
        private Dictionary<string, int> _labels = new Dictionary<string, int>();
        private List<IInstruction> _instructions = new List<IInstruction>();
        private List<string> _sourceLines = new List<string>();

        public void Assemble(string inputFile, string outputFile)
        {
            // Read input file
            _sourceLines = File.ReadAllLines(inputFile).ToList();

            // First pass: collect labels
            FirstPass();

            // Second pass: generate instructions
            SecondPass();

            // Ensure we have at least one instruction
            if (_instructions.Count == 0)
            {
                throw new Exception("No instructions to assemble");
            }

            // Add padding nop instructions if needed to make it a multiple of 4
            int paddingNeeded = 4 - (_instructions.Count % 4);
            if (paddingNeeded < 4)
            {
                for (int i = 0; i < paddingNeeded; i++)
                {
                    _instructions.Add(new Nop());
                }
            }

            // Write output file
            using (var output = new BinaryWriter(File.Open(outputFile, FileMode.Create)))
            {
                // Write magic header: 0xDE, 0xAD, 0xBE, 0xEF
                output.Write((byte)0xDE);
                output.Write((byte)0xAD);
                output.Write((byte)0xBE);
                output.Write((byte)0xEF);

                // Write all instructions
                foreach (var inst in _instructions)
                {
                    // We need to write in little-endian format
                    int encoded = inst.Encode();
                    Console.WriteLine($"{inst.GetType().Name.PadRight(12)} -> 0x{encoded:X8}");
                    output.Write(encoded);
                }
            }
        }

        private void FirstPass()
        {
            int pc = 0; // Program counter (memory address)

            for (int line = 0; line < _sourceLines.Count; line++)
            {
                string currentLine = _sourceLines[line].Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(currentLine))
                    continue;

                // Remove comments
                int commentIndex = currentLine.IndexOf('#');
                if (commentIndex >= 0)
                {
                    currentLine = currentLine.Substring(0, commentIndex).Trim();
                    if (string.IsNullOrWhiteSpace(currentLine))
                        continue;
                }

                // Check if line is a label
                if (currentLine.EndsWith(":"))
                {
                    string label = currentLine.Substring(0, currentLine.Length - 1);
                    if (_labels.ContainsKey(label))
                    {
                        throw new Exception($"Duplicate label '{label}' on line {line + 1}");
                    }
                    _labels[label] = pc;
                }
                // Otherwise, it's an instruction
                else
                {
                    string[] parts = currentLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    string instructionName = parts[0].ToLower();
                    
                    // Handle special case for stpush which expands to multiple instructions
                    if (instructionName == "stpush")
                    {
                        // Extract the string from the line
                        Match match = Regex.Match(currentLine, "stpush\\s+\"(.*)\"");
                        if (match.Success)
                        {
                            string str = match.Groups[1].Value;
                            // Replace escape sequences
                            str = str.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n");
                            
                            // Calculate number of push instructions needed (3 chars per push)
                            int pushCount = (str.Length + 2) / 3; // +2 to round up
                            if (str.Length == 0)
                                pushCount = 1; // Empty string still needs one push for null terminator
                                
                            pc += pushCount * 4; // Each push is 4 bytes
                        }
                        else
                        {
                            throw new Exception($"Invalid stpush format on line {line + 1}");
                        }
                    }
                    else
                    {
                        pc += 4; // Regular instruction (4 bytes)
                    }
                }
            }
        }

        private void SecondPass()
        {
            int currentPC = 0; // Keep track of PC for calculating offsets
            
            for (int line = 0; line < _sourceLines.Count; line++)
            {
                string currentLine = _sourceLines[line].Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(currentLine))
                    continue;

                // Remove comments
                int commentIndex = currentLine.IndexOf('#');
                if (commentIndex >= 0)
                {
                    currentLine = currentLine.Substring(0, commentIndex).Trim();
                    if (string.IsNullOrWhiteSpace(currentLine))
                        continue;
                }

                // Skip labels
                if (currentLine.EndsWith(":"))
                    continue;

                // Parse instruction
                string[] parts = currentLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string opcode = parts[0].ToLower();

                try
                {
                    switch (opcode)
                    {
                        case "exit":
                            {
                                int code = parts.Length > 1 ? ParseInt(parts[1]) : 0;
                                _instructions.Add(new Exit(code));
                                currentPC += 4;
                                break;
                            }
                        case "swap":
                            {
                                int from = parts.Length > 1 ? ParseInt(parts[1]) : 4;
                                int to = parts.Length > 2 ? ParseInt(parts[2]) : 0;
                                _instructions.Add(new Swap(from, to));
                                currentPC += 4;
                                break;
                            }
                        case "nop":
                            {
                                _instructions.Add(new Nop());
                                currentPC += 4;
                                break;
                            }
                        case "input":
                            {
                                _instructions.Add(new Input());
                                currentPC += 4;
                                break;
                            }
                        case "stinput":
                            {
                                uint maxChars = parts.Length > 1 ? (uint)ParseInt(parts[1]) : 0xFFFFFF;
                                _instructions.Add(new StInput(maxChars));
                                currentPC += 4;
                                break;
                            }
                        case "debug":
                            {
                                int value = parts.Length > 1 ? ParseInt(parts[1]) : 0;
                                _instructions.Add(new Debug(value));
                                currentPC += 4;
                                break;
                            }
                        case "pop":
                            {
                                int offset = parts.Length > 1 ? ParseInt(parts[1]) : 4;
                                _instructions.Add(new Pop(offset));
                                currentPC += 4;
                                break;
                            }
                        case "add":
                            _instructions.Add(new BinaryArithmetic(0));
                            currentPC += 4;
                            break;
                        case "sub":
                            _instructions.Add(new BinaryArithmetic(1));
                            currentPC += 4;
                            break;
                        case "mul":
                            _instructions.Add(new BinaryArithmetic(2));
                            currentPC += 4;
                            break;
                        case "div":
                            _instructions.Add(new BinaryArithmetic(3));
                            currentPC += 4;
                            break;
                        case "rem":
                            _instructions.Add(new BinaryArithmetic(4));
                            currentPC += 4;
                            break;
                        case "and":
                            _instructions.Add(new BinaryArithmetic(5));
                            currentPC += 4;
                            break;
                        case "or":
                            _instructions.Add(new BinaryArithmetic(6));
                            currentPC += 4;
                            break;
                        case "xor":
                            _instructions.Add(new BinaryArithmetic(7));
                            currentPC += 4;
                            break;
                        case "lsl":
                            _instructions.Add(new BinaryArithmetic(8));
                            currentPC += 4;
                            break;
                        case "lsr":
                            _instructions.Add(new BinaryArithmetic(9));
                            currentPC += 4;
                            break;
                        case "asr":
                            _instructions.Add(new BinaryArithmetic(11));
                            currentPC += 4;
                            break;
                        case "neg":
                            _instructions.Add(new UnaryArithmetic(0));
                            currentPC += 4;
                            break;
                        case "not":
                            _instructions.Add(new UnaryArithmetic(1));
                            currentPC += 4;
                            break;
                        case "stprint":
                            {
                                int offset = parts.Length > 1 ? ParseInt(parts[1]) : 0;
                                _instructions.Add(new StPrint(offset));
                                currentPC += 4;
                                break;
                            }
                        case "call":
                            {
                                if (parts.Length < 2)
                                    throw new Exception($"Missing label for call instruction on line {line + 1}");
                                
                                string labelName = parts[1];
                                if (!_labels.ContainsKey(labelName))
                                    throw new Exception($"Undefined label '{labelName}' on line {line + 1}");
                                
                                int targetPC = _labels[labelName];
                                int offset = targetPC - currentPC;
                                
                                _instructions.Add(new Call(offset));
                                currentPC += 4;
                                break;
                            }
                        case "return":
                            {
                                int offset = parts.Length > 1 ? ParseInt(parts[1]) : 0;
                                _instructions.Add(new Return(offset));
                                currentPC += 4;
                                break;
                            }
                        case "goto":
                            {
                                if (parts.Length < 2)
                                    throw new Exception($"Missing label for goto instruction on line {line + 1}");
                                
                                string labelName = parts[1];
                                if (!_labels.ContainsKey(labelName))
                                    throw new Exception($"Undefined label '{labelName}' on line {line + 1}");
                                
                                int targetPC = _labels[labelName];
                                int offset = targetPC - currentPC;
                                
                                _instructions.Add(new Goto(offset));
                                currentPC += 4;
                                break;
                            }
                        case "ifeq":
                        case "ifne":
                        case "iflt":
                        case "ifgt":
                        case "ifle":
                        case "ifge":
                            {
                                if (parts.Length < 2)
                                    throw new Exception($"Missing label for {opcode} instruction on line {line + 1}");
                                
                                string labelName = parts[1];
                                if (!_labels.ContainsKey(labelName))
                                    throw new Exception($"Undefined label '{labelName}' on line {line + 1}");
                                
                                int targetPC = _labels[labelName];
                                int offset = targetPC - currentPC;
                                
                                int condCode = 0;
                                switch (opcode)
                                {
                                    case "ifeq": condCode = 0; break;
                                    case "ifne": condCode = 1; break;
                                    case "iflt": condCode = 2; break;
                                    case "ifgt": condCode = 3; break;
                                    case "ifle": condCode = 4; break;
                                    case "ifge": condCode = 5; break;
                                }
                                
                                _instructions.Add(new BinaryIf(condCode, offset));
                                currentPC += 4;
                                break;
                            }
                        case "ifez":
                        case "ifnz":
                        case "ifmi":
                        case "ifpl":
                            {
                                if (parts.Length < 2)
                                    throw new Exception($"Missing label for {opcode} instruction on line {line + 1}");
                                
                                string labelName = parts[1];
                                if (!_labels.ContainsKey(labelName))
                                    throw new Exception($"Undefined label '{labelName}' on line {line + 1}");
                                
                                int targetPC = _labels[labelName];
                                int offset = targetPC - currentPC;
                                
                                int condCode = 0;
                                switch (opcode)
                                {
                                    case "ifez": condCode = 0; break;
                                    case "ifnz": condCode = 1; break;
                                    case "ifmi": condCode = 2; break;
                                    case "ifpl": condCode = 3; break;
                                }
                                
                                _instructions.Add(new UnaryIf(condCode, offset));
                                currentPC += 4;
                                break;
                            }
                        case "dup":
                            {
                                int offset = parts.Length > 1 ? ParseInt(parts[1]) : 0;
                                _instructions.Add(new Dup(offset));
                                currentPC += 4;
                                break;
                            }
                            case "print":
                            case "printh":
                            case "printb":
                            case "printo":
                            {
                                int offset = parts.Length > 1 ? ParseInt(parts[1]) : 0;
                                int format = 0;
                                
                                // Set the format based on the instruction type
                                switch (opcode)
                                {
                                    case "print": format = 0; break;  // Decimal (base 10)
                                    case "printh": format = 1; break; // Hex (base 16)
                                    case "printb": format = 2; break; // Binary (base 2)
                                    case "printo": format = 3; break; // Octal (base 8)
                                }
                                
                                _instructions.Add(new Print(offset, format));
                                currentPC += 4;
                                break;
                            }
                        case "dump":
                            {
                                _instructions.Add(new Dump());
                                currentPC += 4;
                                break;
                            }
                            case "push":
                            {
                                int value = 0;
                                if (parts.Length > 1)
                                {
                                    // Check if it's a label
                                    if (_labels.ContainsKey(parts[1]))
                                    {
                                        value = _labels[parts[1]];
                                    }
                                    else
                                    {
                                        value = ParseInt(parts[1]);
                                    }
                                }
                                
                                _instructions.Add(new Push(value));
                                currentPC += 4;
                                break;
                            }
                            case "stpush":
                            {
                                // Extract the string from the line
                                Match match = Regex.Match(currentLine, "stpush\\s+\"(.*)\"");
                                if (match.Success)
                                {
                                    string str = match.Groups[1].Value;
                                    
                                    // Replace escape sequences
                                    str = str.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n");
                                    
                                    // Pad the string to make it a multiple of 3 characters
                                    int paddingNeeded = (str.Length % 3 == 0) ? 0 : (3 - (str.Length % 3));
                                    string paddedStr = str.PadRight(str.Length + paddingNeeded, (char)1);
                                    
                                    // Reverse the string (critical difference from your implementation)
                                    char[] charArray = paddedStr.ToCharArray();
                                    Array.Reverse(charArray);
                                    string reversedStr = new string(charArray);
                                    
                                    // Process in chunks of 3 characters
                                    int chunkCount = paddedStr.Length / 3;
                                    List<int> values = new List<int>();
                                    
                                    for (int i = 0; i < chunkCount; i++)
                                    {
                                        // Pack 3 characters into a 32-bit value (in correct order from reversed string)
                                        int value = (reversedStr[i * 3] << 16) | 
                                                    (reversedStr[i * 3 + 1] << 8) | 
                                                    (reversedStr[i * 3 + 2]);
                                        
                                        // Add continuation byte (0x01) for all chunks except the first one (which is the last one in the original string)
                                        if (i != 0)
                                        {
                                            value |= (0x01 << 24);
                                        }
                                        
                                        values.Add(value);
                                    }
                                    
                                    // Add the chunks to the instruction list
                                    foreach (int value in values)
                                    {
                                        _instructions.Add(new Push(value));
                                        currentPC += 4;
                                    }
                                }
                                else
                                {
                                    throw new Exception($"Invalid stpush format on line {line + 1}");
                                }
                                break;
                            }
                        default:
                            throw new Exception($"Unknown instruction '{opcode}' on line {line + 1}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error on line {line + 1}: {ex.Message}");
                }
            }
        }

        private int ParseInt(string value)
        {
            value = value.Trim();
            
            // Hexadecimal (0x prefix)
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(value.Substring(2), 16);
            }
            
            // Binary (0b prefix)
            if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(value.Substring(2), 2);
            }
            
            // Decimal
            return int.Parse(value);
        }
    }
}