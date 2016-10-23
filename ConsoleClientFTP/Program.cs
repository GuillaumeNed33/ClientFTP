/*Fait par Guillaume NEDELEC et Antoine POIRIER */
/* Retrait d'un fichier texte et fermeture de la connexion*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace ConsoleClientFTP
{
    class Program
    {
        static bool VERBOSE = false;
        static string username = "anonymous";
        static string password = "tewanetguigui";
        static string nomServeur = "ftp.lip6.fr";


        static void FATAL(string Message)
        {
            Console.WriteLine(Message);
            Console.ReadLine();
            Environment.Exit(-1);
        }

        static void LireLigne(StreamReader input, out string ligne)
        {
            ligne = input.ReadLine();
            if (VERBOSE)
                Console.WriteLine("     recu  >> " + ligne);
        }
        static void EcrireLigne(StreamWriter output, string ligne)
        {
            output.WriteLine(ligne);
            if (VERBOSE)
                Console.WriteLine("     envoi << " + ligne);
        }

        static TcpClient connexion(string nomServeur, int port)
        {
            TcpClient socketClient = new TcpClient();

            IPAddress adresse = IPAddress.Parse("127.0.0.1");
            bool trouve = false;
            IPAddress[] adresses = Dns.GetHostAddresses(nomServeur);
            foreach (IPAddress ip in adresses)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    trouve = true;
                    adresse = ip;
                    break;
                }
            }
            if (!trouve)
            {
                FATAL("Echec recherche IP serveur");
            }
            socketClient.Connect(adresse, port);
            return socketClient;
        }

        static void travail(TcpClient socketClient)
        {
            // Stream pour lecture et écriture
            StreamWriter sw;
            StreamReader sr;

            if (socketClient.Connected)
            {
                // connexion ok, mise en place des streams pour lecture et écriture
                sr = new StreamReader(socketClient.GetStream(), Encoding.Default);
                sw = new StreamWriter(socketClient.GetStream(), Encoding.Default);
                sw.AutoFlush = true;

                string ligne, tampon;

                /* envoi identification */
                tampon = "USER " + username;
                EcrireLigne(sw, tampon);   
                LireLigne(sr, out ligne);
                while (!ligne.StartsWith("331"))
                {                    
                    LireLigne(sr, out ligne);
                }
                Console.WriteLine("USER accepté.");

                /* envoi mot de passe */
                tampon = "PASS " + password;
                EcrireLigne(sw, tampon);
                LireLigne(sr, out ligne);
                while(!ligne.StartsWith("230 Guest login ok"))
                {
                    if (ligne.StartsWith("530"))
                    {
                        FATAL("PASS rejeté. Abandon");
                    }
                    LireLigne(sr, out ligne);
                }
                Console.WriteLine("Password accepté. Vous êtes connecté.");

                /*Passage en mode passif */
                tampon = "PASV";
                EcrireLigne(sw, tampon);
                LireLigne(sr, out ligne);

                /*recuperation de l'adresse et du port*/
                string[] champ = ligne.Split('(');

                /*decoupe de chaque données des infos precedentes*/
                string[] tab = champ[1].Split(',');

                /*Conversion en entier dans un tableau d'entier*/
                int[] adresse = new int[6];
                
                /* Retrait de la parenthese finale*/
                string[] last = tab[5].Split(')');
                tab[5] = last[0];
                
                int i=0;
       
                /*Recuperation de l'adresse*/
                foreach(string s in tab)
                {
                    adresse[i] = Int32.Parse(s);
                    i++;
                }
                Console.WriteLine("\nMode Passif activé.");

                /*Calcul du port*/
                int port_connect = ((adresse[4] * 256) + (adresse[5])); 
                
                /*Definition du serveur */
                string new_nomServeur = adresse[0]+"."+adresse[1]+"."+adresse[2]+"."+adresse[3];
                
                /*envoi de la commande */
                tampon = "RETR /pub/games/xpilot/distrib/README";
                EcrireLigne(sw, tampon);
               
                /*Lecture de la réponse */
                secondeConnexion(port_connect, new_nomServeur);  
             
                /* Lecture dans le premier socket -- Verification transfert */
                LireLigne(sr, out ligne);
                while (!ligne.StartsWith("226") && !ligne.StartsWith("550"))
                {
                    LireLigne(sr, out ligne);
                }
                if (ligne.StartsWith("550"))
                {
                    FATAL("Fichier introuvable. Abandon");
                }
            }
        }

        static void secondeConnexion(int port_connect, string new_nomServeur)
        {
            Console.WriteLine("\nOuverture d'une seconde Connexion...");
            /*Ouverture d'une seconde connexion */
            TcpClient socketClient_2 = connexion(new_nomServeur, port_connect);

            StreamReader sr2 = new StreamReader(socketClient_2.GetStream(), Encoding.Default);
            StreamWriter sw2 = new StreamWriter(socketClient_2.GetStream(), Encoding.Default);
            sw2.AutoFlush = true;

            string ligne;

            Console.WriteLine("Contenu du document '/pub/games/xpilot/distrib/README' :\n");
            
            /*Lecture du contenu envoyé par le premier serveur */
            string message = "";
            LireLigne(sr2, out ligne);
            while (ligne!=null)
            {
                message += "\t"+ligne + "\n";
                LireLigne(sr2, out ligne);
            }
            message += "\t" +ligne;
            Console.WriteLine(message);
            Console.WriteLine("\nFermeture de la seconde connexion");

            socketClient_2.Close();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Démarrage du client\n");
            TcpClient socketClient;

            int port = 21;
            socketClient = connexion(nomServeur, port);
            travail(socketClient);

            socketClient.Close();
            Console.WriteLine("Fin du client -> Taper une touche pour terminer");
            Console.ReadLine();
        }
    }
}
