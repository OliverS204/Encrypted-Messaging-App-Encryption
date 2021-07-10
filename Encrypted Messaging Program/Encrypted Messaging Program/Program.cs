﻿using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Linq;


//{ p, g, A, B}
namespace Encrpytion_Prototype
{
    public class KeyData
    {
        public String Data { get; set; }
        public BigInteger prime { get; set; }
        public int g { get; set; }
        public BigInteger A { get; set; }
        public BigInteger B { get; set; }
        public KeyData(BigInteger prime_, int g_, BigInteger secret, int userNum)
        {
            prime = prime_;
            g = g_;
            if(userNum == 0){
                A = secret;
            }
            else{
                B = secret;
            }
        }
    }



    public class Program
    {
        public FirebaseClient firebase = new FirebaseClient("https://messaging-app-demo-348e5-default-rtdb.europe-west1.firebasedatabase.app/");
        public static void Main(string[] args)
        {
            Console.WriteLine("\nInitilising DiffieHellman");

            Program test = new Program();
            test.testRequest().Wait();
            Console.WriteLine("Test finished.....");

            //new Program().SendRequest(userID, data).Wait();

        }

        private async Task testRequest()
        {
            Console.Write("Enter your user ID:  ");
            string userID = Console.ReadLine();
            //new Program().GetRequests(userID).Wait();
            Request Requests = new Request(firebase, userID);


            bool stop = false;
            while (!stop)
            {
                Console.WriteLine("--Menu--\n1) Send Request\n2) Accept Request\n3) Change user\n");
                string choice = Console.ReadLine();
                bool success = Int32.TryParse(choice, out int int_choice);
                if (success && int_choice <= 3 && int_choice > 0)
                {
                    switch (int_choice)
                    {
                        case 1:
                            Console.Write("Enter user:  ");
                            string requestUser = Console.ReadLine();
                            DiffieHellman user1 = new DiffieHellman(256);

                            await SendRequest(userID, requestUser, user1.Initilise());
                            break;
                        case 2:
                            Console.WriteLine("Pending Requests:");


                            string[] requestID = await Requests.GetAll();
                            foreach (string request in requestID)
                            {
                                Console.WriteLine(request);
                            }


                            Console.WriteLine();
                            Console.WriteLine("Enter user to accept: ");
                            string acceptUser = Console.ReadLine();
                            if (acceptUser.Length > 0)
                            {
                                string Data = Requests.GetData(acceptUser);
                                Console.WriteLine($"Done: {Data}");
                            }
                            break;
                    }
                }
            }
        }

        private void testDH()
        {
            //Dictionary<string, BigInteger[]> Server = new Dictionary<string, BigInteger[]>();


            Console.WriteLine("User 1  (a)");
            DiffieHellman user1 = new DiffieHellman(256, 'a', true);
            Console.WriteLine("User 2  (b)");
            DiffieHellman user2 = new DiffieHellman(256, 'b', true);

            Console.WriteLine();

            KeyData data = user1.Initilise(); // Send Friend Request
            //Server.Add(userID, data);
            user2.Respond(data);
            BigInteger secret2 = user2.getSharedKey(data, 1);
            BigInteger secret1 = user1.getSharedKey(data, 0);
            Console.WriteLine(secret1 == secret2);
        }

        public async Task<string[]> GetRequests(string userID)
        {
            var items = firebase.Child("users").Child(userID).Child("requests").OnceAsync<KeyData>();

            var requestID = new List<string>();

            foreach (var pair in await items)
            {
                //Console.WriteLine($"{pair.Key} : {pair.Object}");
                requestID.Add(pair.Key);
            }
            return requestID.ToArray();
        }

        /*public async Task<BigInteger[]> GetRequest(string userID, string requestID)
        {
            var items = firebase.Child("users").Child(userID).Child("requests").Child("").OnceAsync<KeyData>();

            return items[requestID];
            //https://bolorundurowb.com/posts/31/using-the-firebase-realtime-database-with-.net
        }*/


        private async Task SendRequest(string userID, String requestID, BigInteger[] data)
        {
            await firebase.Child("users").Child(requestID).Child("requests").Child(userID).PostAsync(data);
            Console.WriteLine("Done");
        }

    }

    class Request
    {
        Dictionary<string, KeyData> requests = new Dictionary<string, KeyData>();
        String userID;
        public FirebaseClient firebase;
        public Request(FirebaseClient p_firebase, string p_userID)
        {
            userID = p_userID;
            firebase = p_firebase;
            fillRequests(userID);


        }

        private async Task fillRequests(string userID){
            var child = firebase.Child("users").Child(userID).Child("requests");
            var observable = child.AsObservable<KeyData>();
            var items = await child.OnceAsync<KeyData>();
            requests.Clear();
            //Object[] types = items.GetType().GetMethods();
            //Console.WriteLine(String.Join("\n", types));
            //foreach (var item in items)
            //{
                //requests.Add(item.Key, item.Object);
                //Console.WriteLine($"{item.Key}: {item.Object}");
            //}
            var subscription = observable
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .Where(x => !requests.ContainsKey(x.Key))
                .Subscribe(s => requests.Add(s.Key, s.Object));
        }

        public async Task<String[]> GetAll() //<List<KeyValuePair<string, KeyData>>>
        {
            if(requests.Count == 0){
                Console.WriteLine("Requests incomplete, fetching again");
                await fillRequests(userID);
            }
            return requests.Keys.ToArray();
        }

        public string GetData(string requestID)
        {
            return requests[requestID].Data;
        }

    }



    class DiffieHellman
    {
        private BigInteger prime;
        private BigInteger global;
        private BigInteger secret;

        private RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private Random rnd = new Random();

        private int[] identifiers = { 5, 14, 15, 16, 17, 18 };
        private string[] primes =  {
            "0FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA237327FFFFFFFFFFFFFFFF",
            "0FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AACAA68FFFFFFFFFFFFFFFF",
            "0FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200CBBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFCE0FD108E4B82D120A93AD2CAFFFFFFFFFFFFFFFF",
            "0FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200CBBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D788719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA993B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934063199FFFFFFFFFFFFFFFF",
            "0FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200CBBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D788719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA993B4EA988D8FDDC186FFB7DC90A6C08F4DF435C93402849236C3FAB4D27C7026C1D4DCB2602646DEC9751E763DBA37BDF8FF9406AD9E530EE5DB382F413001AEB06A53ED9027D831179727B0865A8918DA3EDBEBCF9B14ED44CE6CBACED4BB1BDB7F1447E6CC254B332051512BD7AF426FB8F401378CD2BF5983CA01C64B92ECF032EA15D1721D03F482D7CE6E74FEF6D55E702F46980C82B5A84031900B1C9E59E7C97FBEC7E8F323A97A7E36CC88BE0F1D45B7FF585AC54BD407B22B4154AACC8F6D7EBF48E1D814CC5ED20F8037E0A79715EEF29BE32806A1D58BB7C5DA76F550AA3D8A1FBFF0EB19CCB1A313D55CDA56C9EC2EF29632387FE8D76E3C0468043E8F663F4860EE12BF2D5B0B7474D6E694F91E6DCC4024FFFFFFFFFFFFFFFF",
            "0FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF6955817183995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200CBBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D788719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA993B4EA988D8FDDC186FFB7DC90A6C08F4DF435C93402849236C3FAB4D27C7026C1D4DCB2602646DEC9751E763DBA37BDF8FF9406AD9E530EE5DB382F413001AEB06A53ED9027D831179727B0865A8918DA3EDBEBCF9B14ED44CE6CBACED4BB1BDB7F1447E6CC254B332051512BD7AF426FB8F401378CD2BF5983CA01C64B92ECF032EA15D1721D03F482D7CE6E74FEF6D55E702F46980C82B5A84031900B1C9E59E7C97FBEC7E8F323A97A7E36CC88BE0F1D45B7FF585AC54BD407B22B4154AACC8F6D7EBF48E1D814CC5ED20F8037E0A79715EEF29BE32806A1D58BB7C5DA76F550AA3D8A1FBFF0EB19CCB1A313D55CDA56C9EC2EF29632387FE8D76E3C0468043E8F663F4860EE12BF2D5B0B7474D6E694F91E6DBE115974A3926F12FEE5E438777CB6A932DF8CD8BEC4D073B931BA3BC832B68D9DD300741FA7BF8AFC47ED2576F6936BA424663AAB639C5AE4F5683423B4742BF1C978238F16CBE39D652DE3FDB8BEFC848AD922222E04A4037C0713EB57A81A23F0C73473FC646CEA306B4BCBC8862F8385DDFA9D4B7FA2C087E879683303ED5BDD3A062B3CF5B3A278A66D2A13F83F44F82DDF310EE074AB6A364597E899A0255DC164F31CC50846851DF9AB48195DED7EA1B1D510BD7EE74D73FAF36BC31ECFA268359046F4EB879F924009438B481C6CD7889A002ED5EE382BC9190DA6FC026E479558E4475677E9AA9E3050E2765694DFC81F56E880B96E7160C980DD98EDD3DFFFFFFFFFFFFFFFFF"
        };

        private bool debug;
        private char user_char;

        public DiffieHellman(int secret_size = 256, char p_char = '\0', bool p_debug = false)   // Generate secret number
        {
            //Secret
            byte[] b_secret = new byte[secret_size];
            rngCsp.GetBytes(b_secret);
            secret = BitConverter.ToUInt32(b_secret, 0);
            debug = p_debug;
            user_char = p_char;
            if (debug)
            {
                Console.WriteLine($"Secret({user_char}): {secret}");
            }
        }

        public KeyData Initilise(int p_id = 14, int g = 5)       // Choose p,g and a
        {
            // Prime (p): 
            prime = getPrime(p_id);
            Console.WriteLine($"Prime(p): {prime.ToByteArray().Length} bytes");

            //Base (g):
            global = g;
            Console.WriteLine($"Base(g): {g}");

            //A: Calculate public key
            BigInteger publicKey = getPublicKey();

            return new KeyData(prime, g, publicKey, 0);
            //return new BigInteger[] { prime, global, publicKey, 0 }; //{p, g, A, B}
        }

        public KeyData Respond(KeyData data)         // Uses pre-calculated values for p and g to get B
        {
            prime = data.prime;
            global = data.g;

            data.B = getPublicKey();
            return data;

        }

        public BigInteger getSharedKey(KeyData data, int user)  // User 1: B^a  mod  p        User 2: A^b  mod  p      {p,g,A,B}
        {
            BigInteger request_secret;
            if (user == 0) { request_secret = data.B; }
            else { request_secret = data.A; }
            return BigInteger.ModPow(request_secret, secret, prime);
        }

        private BigInteger getPrime(int p_id)
        {
            int prime_num = Array.IndexOf(identifiers, p_id);
            if (prime_num == -1) { prime_num = 1; }

            return BigInteger.Parse(primes[prime_num], System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        private BigInteger getPublicKey()
        {
            BigInteger publicKey = BigInteger.ModPow(global, secret, prime);
            if (debug) { Console.WriteLine($"\nPublic Key Calculated ({user_char}):"); }
            Console.WriteLine(publicKey);
            return publicKey;
        }


    }
}

//-r:System.Numerics.dll


