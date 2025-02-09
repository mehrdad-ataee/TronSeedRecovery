using System.Text;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Org.BouncyCastle.Crypto.Digests;


public class TronSeedRecoveryTest
{
    private static List<string> ReadWordsFromFile(string fileName = "wordslist.txt")
    {
        string words_string = File.ReadAllText(@$"Resources\{fileName}");

        return [.. words_string.Split(',')];
    }

    private static int GetWordIndex(string word, List<string> words)
    {
        return words.IndexOf(word);
    }

    private static string ToBinaryString(int index, int length = 11)
    {
        return Convert.ToString(index, 2).PadLeft(length, '0'); 
    }

    private static byte[] BinaryToByte(string binaryStr)
    {
        byte[] byte_array = new byte[binaryStr.Length / 8];
        for (int i = 0; i < byte_array.Length; i++)
        {
            byte_array[i] = Convert.ToByte(binaryStr.Substring(i * 8, 8), 2);
        }
        return byte_array;
    }

    public static string BinaryToWord(string binary_chunk_str, List<string> words_list)
    {
        int index = Convert.ToInt32(binary_chunk_str, 2);
        return words_list[index];
    }

    public static string[] FullBinaryToChunks(string binary_full_str)
    {
        int words_number = binary_full_str.Length / 11;
        string[] binary_chunks = new string[words_number];
        for (int i = 0; i < words_number; i++)
        {
            binary_chunks[i] = binary_full_str.Substring(i * 11, 11);
        }
        return binary_chunks;
    }

    public static string ByteToBitString(byte b)
    {
        StringBuilder sb = new StringBuilder(8);
        for (int i = 7; i >= 0; i--) // Iterate from most significant bit to least significant
        {
            sb.Append((b & (1 << i)) == 0 ? '0' : '1');
        }
        return sb.ToString();
    }

    
    static byte[] HashPublicKey(byte[] publicKey)
    {
        // Step 1: Hash the public key using Keccak-256
        var keccak = new KeccakDigest(256);
        keccak.BlockUpdate(publicKey, 1, publicKey.Length-1);
        byte[] hash = new byte[32];
        keccak.DoFinal(hash, 0);

        // Step 2: Take the last 20 bytes of the hash
        byte[] addressBytes = new byte[21];
        addressBytes[0] = 0x41; // Tron address prefix (T)
        Array.Copy(hash, hash.Length - 20, addressBytes, 1, 20);

        
        // Step 3: Encode the address using Base58Check
        return addressBytes;
    }


    public static void Main(string[] args)
    {
        List<string> all_words = ReadWordsFromFile();
        List<string> known_words = ReadWordsFromFile("known_words.txt");
        string target_address = File.ReadAllText(@$"Resources\target_address.txt");
        string derivation_path = File.ReadAllText(@$"Resources\derivation_path.txt");
        int unknown_words_count = known_words.Count <= 12 ? 12 - known_words.Count : 24 - known_words.Count;

        string knwon_binary = "";

        for (int i = 0; i < known_words.Count; i++)
        {
            int index = GetWordIndex(known_words[i], all_words);
            string eleven_bit_binary = ToBinaryString(index, 11);
            knwon_binary += eleven_bit_binary;
        }

        int unknown_bits_count = (unknown_words_count * 11) - 4; //last 4 is checksum and does not affect address generation
        double combinations = Math.Pow(2, unknown_bits_count);
        Console.WriteLine($"▐ unknown words count [{unknown_words_count}]\n"+
                          $"▐ knwon binary [{knwon_binary}]\n"+
                          $"▐ combinations[{combinations}]\n"+
                           " ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
        for (int i = 0; i < combinations; i++)
        {
            string unknown_binary = unknown_words_count>0 ? Convert.ToString(i, 2).PadLeft(unknown_bits_count, '0') : "";
            string full_binary = knwon_binary + unknown_binary;
            string entropy_bits = full_binary[0..128];
            string checksum_org = full_binary[128..^0];

            byte[] entropy_bytes = BinaryToByte(entropy_bits);
            Mnemonic mnemonic = new Mnemonic(Wordlist.English ,entropy_bytes);

            ExtKey masterKey = mnemonic.DeriveExtKey();

            // Tron derivation path: m/44'/195'/0'/0/0
            ExtKey tronKey = masterKey.Derive(new KeyPath(derivation_path));
            
            
            byte[] publicKeyBytes = tronKey.GetPublicKey().Decompress().ToBytes();
            byte[] publicKeyBytesHash = HashPublicKey(publicKeyBytes);

            byte[] checksum = Hashes.SHA256(Hashes.SHA256(publicKeyBytesHash)).Take(4).ToArray();
            byte[] fullAddressBytes = publicKeyBytesHash.Concat(checksum).ToArray();

            string tronAddress = Encoders.Base58.EncodeData(fullAddressBytes);
            if (tronAddress == target_address)
            {
                byte[] last4_checksum = Hashes.SHA256(entropy_bytes);
                string last4_binary = ByteToBitString(last4_checksum[0]);

                string[] binary_chunks = FullBinaryToChunks(full_binary+last4_binary);
                string seeds = "";
                for (int j = 0; j < binary_chunks.Length; j++)
                {
                    seeds += BinaryToWord(binary_chunks[j], all_words);
                    if (j < binary_chunks.Length - 1)
                    {
                        seeds += ",";
                    }
                }

                Console.WriteLine($"▐ Index [{i}]\n"+
                                  $"▐ tronAddress [{tronAddress}]\n"+
                                  $"▐ binary|chcksm [{full_binary}|{last4_binary[0..4]}]\n"+
                                  $"▐ Found Seed Phrase -> [{seeds}]");
                Console.ReadKey();
            }
        }
    }
}