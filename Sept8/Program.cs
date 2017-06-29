using System;
using System.IO;
using System.Threading;

namespace TestShite
{
    class Emulator
    {
        struct Chip8
        {
            #region Chip 8 font set
            public static byte[] font_set = new byte[80]
            {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };
            #endregion

            #region Memory, stack(and pointer), registers, index, framebuffer, program counter
            public static bool isInitialized = false;
            public static short opcode;
            public static byte[] Memory = new byte[4096]; //this is memory allocated for the system.
            public static byte[] V = new byte[16]; //15 8-bit purpose registers, last is carry flag
            public static short I; //index register
            public static short pc; //program counter   
            public static byte[] gfx = new byte[64 * 32]; //framebuffer, 64 by 32 pixels is default resolution for chip8
            public static byte delay_timer;
            public static byte sound_timer;
            public static short[] stack = new short[16]; //16 levels of stack
            public static short sp; //stack pointer to remember where we jumped from
            public static string romName;

            //Chip-8 Memory Map
            //0x000 - 0x01FF : Interpreter
            //0x050 - 0x0A0 : Font set
            //0x200 - 0xFFF : Program data      
            #endregion
        }

        struct Atari2600
        {
        }

        struct ZXSpectrum
        {
        }

        class CPU_Atari2600
        {
        }

        class CPU_ZXSpectrum
        {
        }

        class CPU_Chip8
        {
            public void Initialize()
            {
                Chip8.pc = 0x200; //The program always starts at this offset.
                Chip8.opcode = 0; //We are at the first opcode.
                Chip8.I = 0; //Set index register.
                Chip8.sp = 0; //Set stack pointer.

                for (int i = 0; i < 80; i++)
                {
                    Chip8.Memory[i] = Chip8.font_set[i]; //Load fontset into memory.
                }

                Chip8.delay_timer = 0;
                Chip8.sound_timer = 0;
                Chip8.isInitialized = true;
            }

            public void LoadROM(string filename)
            {
                Chip8.romName = filename;
                StreamReader sr = new StreamReader(filename);
                for (int i = 0; i < sr.BaseStream.Length; i++)
                {
                    Chip8.Memory[i + 512] = (byte)sr.BaseStream.ReadByte(); //load each byte into memory at 0x200 and above.
                }
                sr.Close();
                Console.WriteLine("Program successfully loaded: " + filename); //hek ye
            }

            public void emulateCycle()
            {
                #region Opcode fetching and decoding
                var fetchedopcode = Chip8.Memory[Chip8.pc] << 8 | Chip8.Memory[Chip8.pc + 1];
                Chip8.opcode = (short)fetchedopcode;
                Console.Clear();
                int usedMemory = 0;
                foreach (byte value in Chip8.Memory)
                {
                    if (value != 0)
                    {
                        usedMemory += 1;
                    }
                }

                #region Blackjack and hookers
                int nibba = 0;
                int line = 0;
                string memoryLabel = "|LOADED ROM: " + Chip8.romName + "|-|MEMORY: " + usedMemory + "B/4096B|";

                Console.Write("*");
                for (int i = 0; i <= ((192) - (memoryLabel.Length));i++)
                {
                    Console.Write("-");
                    if (i == ((192 / 2) - memoryLabel.Length / 2))
                    {
                        Console.Write(memoryLabel);
                    }
                }
                Console.Write("*\n| ");

                foreach(byte _byte in Chip8.Memory)
                {
                    if (nibba != 63)
                    {
                        Console.Write(_byte.ToString("X2") + " ");
                    }
                    else if(line == 63)
                    {
                        Console.Write(_byte.ToString("X2") + " |\n");
                        line++;
                        nibba = -1;
                    }
                    else
                    {
                        Console.Write(_byte.ToString("X2") + " |\n| ");
                        line++;
                        nibba = -1;
                    }
                    nibba++;
                }

                Console.Write("*");
                for (int i = 0; i < 193; i++)
                {
                    Console.Write("-");
                }
                Console.WriteLine("*\n");

                Console.Write("REGISTERS: ");
                foreach(byte register in Chip8.V)
                {
                    Console.Write(register.ToString("X2") + " ");
                }
                Console.WriteLine();
                #endregion

                Console.WriteLine("fetched opcode: 0x" + Chip8.opcode.ToString("X2"));
                //we now need to determine what the opcode is, and what it's supposed to do
                //ex: 0xA2F0 means to store 0x2F0 into I (0xA000 is therefore the instruction)
                Console.WriteLine("decoded opcode: 0x" + (Chip8.opcode >> 12 & 0xF).ToString("X"));
                #endregion
                #region CPU_Chip8 core and opcode interpretation
                int temp_int; //Because C# sucks, maybe there's a better way but idc
                switch (Chip8.opcode >> 12 & 0xF) //decode opcode, shift 12 bits to the right as first 4 indicate instruction/instruction group
                {
                    case 0x0:
                        {
                            switch (Chip8.opcode & 0xF) //sometimes we can't depend on the first 4 bits to tell us what the opcode is, so we check the last 4. Implement this solution for other opcodes like 0x8000(register operations).
                            {
                                case 0x0: // CLS 
                                    break;
                                case 0xE: // RET 
                                    Chip8.pc = Chip8.stack[Chip8.sp];
                                    --Chip8.sp;
                                    break;
                                default:
                                    Console.WriteLine("Unimplemented opcode: " + Chip8.opcode.ToString("X2"));
                                    break;
                            }
                            break;
                        }
                    case 0x1: // 1nnn - JP addr
                        temp_int = Chip8.opcode & 0x0FFF;
                        Chip8.pc = (short)temp_int;
                        break;
                    case 0x2: //2nnn - CALL addr
                        temp_int = Chip8.opcode & 0x0FFF;
                        Chip8.stack[Chip8.sp] = Chip8.pc; //set the program counter to the top of the stack.
                        Chip8.sp++;
                        Chip8.pc = (short)temp_int;
                        break;
                    case 0x3: //3xkk - SE Vx, byte
                        if ((Chip8.V[Chip8.opcode & 0xF00]) == (Chip8.opcode & 0xFF))
                            Chip8.pc += 4; //Skip next instruction
                        else
                            Chip8.pc += 2; //Otherwise, proceed.
                        break;
                    case 0x4: //4xkk - SNE Vx, byte 
                        if ((Chip8.V[Chip8.opcode & 0xF000]) != (Chip8.opcode & 0xFF))
                            Chip8.pc += 4; //Skip next instruction
                        else
                            Chip8.pc += 2; //Otherwise, proceed.
                        break;
                    case 0x5: //5xy0 - SE Vx, Vy
                        if ((Chip8.V[Chip8.opcode & 0x0F]) == Chip8.V[Chip8.opcode & 0x00F])
                            Chip8.pc += 4; //Skip next instruction
                        else
                            Chip8.pc += 2; //Otherwise, proceed.
                        break;
                    case 0xA: //Annn - LD I, addr
                        temp_int = Chip8.opcode & 0x0FFF;
                        Chip8.I = (short)temp_int;
                        Chip8.pc += 2; // Proceed to next opcode.
                        break;
                    case 0x8:
                        {
                            switch (Chip8.opcode & 0xF)
                            {
                                case 0x0: // 8xy0 - LD Vx, Vy
                                    Chip8.V[Chip8.opcode & 0x0F] = Chip8.V[Chip8.opcode & 0x00F];
                                    Chip8.pc += 2;
                                    break;
                                case 0x1: // 8xy1 - OR Vx, Vy
                                    temp_int = Chip8.V[Chip8.opcode & 0x0F] ^ Chip8.V[Chip8.opcode & 0x00F];
                                    Chip8.V[Chip8.opcode & 0x0F] = (byte)temp_int;
                                    Chip8.pc += 2;
                                    break;
                                /*case 0x2: // 8xy2 - AND Vx, Vy
                                    //Chip8.V[Chip8.opcode & 0x0F] = Chip8
                                    break;
                                case 0x3: // 8xy3 - XOR Vx, Vy
                                    break;
                                case 0x4: // 8xy4 - ADD Vx, Vy
                                    break;
                                case 0x5: // 8xy5 - SUB Vx, Vy
                                    break;
                                case 0x6: // SHR Vx {, Vy}
                                    break;
                                case 0x7: // SUBN Vx, Vy
                                    break;
                                case 0xE: // SHL Vx {, Vy}
                                    break;*/
                                default:
                                    Console.WriteLine("Critical error: Program tried to perform illegal operation 0x" + (Chip8.opcode).ToString("X2"));
                                    /*for (int i = 0; i < 5; i++)
                                    {
                                        Console.Beep();
                                    }*/
                                    //Chip8.pc += 2;
                                    Console.WriteLine("Unimplemented opcode: 0x" + Chip8.opcode.ToString("X2"));
                                    break;
                            }
                            break;
                        }
                    default:
                        //Console.Clear();
                        Console.WriteLine("Critical error: Program tried to perform illegal operation 0x" + (Chip8.opcode).ToString("X2"));
                        /*for(int i = 0; i < 5; i++)
                        {
                            Console.Beep();
                        }*/
                        //Chip8.pc += 2;
                        Console.WriteLine("Unimplemented opcode: 0x" + Chip8.opcode.ToString("X2"));
                        break;
                }
                #endregion

                //do timer shenanigans stuff that literally does pretty much nothing right now
                #region Timers
                if (Chip8.delay_timer > 0)
                    --Chip8.delay_timer;

                if (Chip8.sound_timer > 0)
                {
                    if (Chip8.sound_timer == 1)
                        Console.WriteLine("BEEP");
                    --Chip8.sound_timer;

                }
                #endregion
            }
        }

        static int Main(string[] args)
        {
            Console.Title = "Sept8";
#if !DEBUG
            if(args.Length == 0)
            {
                Console.WriteLine("Please define a ROM file");
                Console.WriteLine("Usage: sept8.exe <ROM file>");
                return 1;
            }
#endif
            CPU_Chip8 sept8 = new CPU_Chip8();
            do
            {
                if (!Chip8.isInitialized)
                {
                    sept8.Initialize();
                    try
                    {
#if !DEBUG
                        sept8.LoadROM(args[0]);
#endif
#if DEBUG
                        sept8.LoadROM("BLINKY"); //our test game
#endif
                        //temporary CPU_Chip8 loop until CPU_Chip8 core is fully implemented
                        do
                        {
                            sept8.emulateCycle();
                            Thread.Sleep(5000);
                        } while (true);
                    }
                    catch (Exception kek)
                    {
                        Console.WriteLine("Failed to load rom file: " + kek.Message);
                    }
                }
            } while (true);
        }
    }
}
