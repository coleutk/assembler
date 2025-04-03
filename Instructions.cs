using System;

namespace VirtualMachineAssembler
{
    // Miscellaneous Instructions (opcode=0)
    public class Exit : IInstruction
    {
        private readonly int _code;

        public Exit(int code)
        {
            _code = code & 0xFF; // 5 bits for code
        }

        public int Encode()
        {
            return (0 << 28) | (0 << 24) | _code;
        }
    }

    public class Swap : IInstruction
    {
        private readonly int _from;
        private readonly int _to;

        public Swap(int from, int to)
        {
            // According to specs, the offset must be divided by 4 since only multiples of 4 are valid
            _from = (from >> 2) & 0xFFF; // 12 bits
            _to = (to >> 2) & 0xFFF;     // 12 bits
        }

        public int Encode()
        {
            return (0 << 28) | (1 << 24) | (_from << 12) | _to;
        }
    }

    public class Nop : IInstruction
    {
        public int Encode()
        {
            return (0 << 28) | (2 << 24);
        }
    }

    public class Input : IInstruction
    {
        public int Encode()
        {
            return (0 << 28) | (4 << 24);
        }
    }

    public class StInput : IInstruction
    {
        private readonly uint _maxChars;

        public StInput(uint maxChars)
        {
            _maxChars = maxChars & 0xFFFFFF; // 24 bits
        }

        public int Encode()
        {
            return (int)((0 << 28) | (5 << 24) | _maxChars);
        }
    }

    public class Debug : IInstruction
    {
        private readonly int _value;

        public Debug(int value)
        {
            _value = value & 0xFFFFFF; // 24 bits
        }

        public int Encode()
        {
            return (0 << 28) | (15 << 24) | _value;
        }
    }

    // Pop Instructions (opcode=1)
    public class Pop : IInstruction
    {
        private readonly int _offset;

        public Pop(int offset)
        {
            _offset = offset & 0x3FFFFFF; // 26 bits
        }

        public int Encode()
        {
            return (1 << 28) | _offset;
        }
    }

    // Binary Arithmetic Instructions (opcode=2)
    public class BinaryArithmetic : IInstruction
    {
        private readonly int _subop;

        public BinaryArithmetic(int subop)
        {
            _subop = subop & 0xF;
        }

        public int Encode()
        {
            return (2 << 28) | (_subop << 24);
        }
    }

    // Unary Arithmetic Instructions (opcode=3)
    public class UnaryArithmetic : IInstruction
    {
        private readonly int _subop;

        public UnaryArithmetic(int subop)
        {
            _subop = subop & 0xF;
        }

        public int Encode()
        {
            return (3 << 28) | (_subop << 24);
        }
    }

    // String Print Instructions (opcode=4)
    public class StPrint : IInstruction
    {
        private readonly int _offset;

        public StPrint(int offset)
        {
            _offset = offset;
        }

        public int Encode()
        {
            return (4 << 28) | (_offset & 0x0FFFFFFC);
        }
    }

    // Call Instructions (opcode=5)
    public class Call : IInstruction
    {
        private readonly int _offset;

        public Call(int offset)
        {
            _offset = offset;
        }

        public int Encode()
        {
            return (5 << 28) | (_offset & 0x0FFFFFFF);
        }
    }

    // Return Instructions (opcode=6)
    public class Return : IInstruction
    {
        private readonly int _offset;

        public Return(int offset)
        {
            _offset = offset;
        }

        public int Encode()
        {
            return (6 << 28) | (_offset & 0x0FFFFFFF);
        }
    }

    // Unconditional Goto Instructions (opcode=7)
    public class Goto : IInstruction
    {
        private readonly int _offset;

        public Goto(int offset)
        {
            _offset = offset;
        }

        public int Encode()
        {
            return (0b0111 << 28) | (_offset & 0x0FFFFFFF);
            // return (7 << 28) | ((_offset & 0x3FFFFFF) << 2);
        }
    }

// Binary If Instructions (opcode=8)
public class BinaryIf : IInstruction
{
    private readonly int _condition;
    private readonly int _offset;

    public BinaryIf(int condition, int offset)
    {
        _condition = condition & 0x7;
        _offset = offset;
    }

    public int Encode()
    {
        // Ensure bits 1-0 are cleared in the offset (PC-relative offset must be multiple of 4)
        return (8 << 28) | (_condition << 25) | (_offset & 0x01FFFFFC);
    }
}

// Unary If Instructions (opcode=9)
public class UnaryIf : IInstruction
{
    private readonly int _condition;
    private readonly int _offset;

    public UnaryIf(int condition, int offset)
    {
        _condition = condition & 0x3;
        _offset = offset;
    }

    public int Encode()
    {
        // Condition is 2 bits at positions 25-26, with bit 27 being 0
        // Ensure bits 1-0 are cleared in the offset (PC-relative offset must be multiple of 4)
        return (9 << 28) | (_condition << 25) | (_offset & 0x01FFFFFC);
    }
}


    // Dup Instructions (opcode=12)
    public class Dup : IInstruction
    {
        private readonly int _offset;

        public Dup(int offset)
        {
            _offset = offset;
        }

        public int Encode()
        {
            return (12 << 28) | (_offset & 0x0FFFFFFF);
        }
    }

    // Print Instructions (opcode=13)
    // Print Instructions (opcode=13)
    public class Print : IInstruction
    {
        private readonly int _offset;
        private readonly int _format;

        public Print(int offset, int format)
        {
            _offset = offset;
            _format = format & 0x3;
        }

        public int Encode()
        {
            // Use the same bitwise masking as in the provided implementation
            return (13 << 28) | (_offset & 0x0ffffffc) | _format;
        }
    }

    // Dump Instructions (opcode=14)
    public class Dump : IInstruction
    {
        public int Encode()
        {
            return (14 << 28);
        }
    }

    // Push Instructions (opcode=15)
    public class Push : IInstruction
    {
        private readonly int _value;

        public Push(int value)
        {
            _value = value & 0x0FFFFFFF; // 28 bits
        }

        public int Encode()
        {
            return (15 << 28) | _value;
        }
    }
}