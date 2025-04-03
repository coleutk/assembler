using System;

namespace VirtualMachineAssembler
{
    // Interface for all instruction implementations
    public interface IInstruction
    {
        // Method to encode an instruction into its 32-bit representation
        int Encode();
    }
}