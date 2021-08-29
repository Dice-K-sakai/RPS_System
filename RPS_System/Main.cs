using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RPS_System;

namespace RPS_System
{
    class main
    {
        static ChatSystem chatSystem;
        const Int32 portNo = 11000;
        const string EOF = "<EOF>";
        static readonly int maxLength = 200 + EOF.Length;
        static ChatSystem.ConnectMode connectMode;

        static string received = "";
        static string user_name = "";

        static bool isBot = false;

        static Random rand = new Random();

        static void Main(string[] args)
        {

            chatSystem = new ChatSystem(maxLength);
            Console.WriteLine($"このPCのホスト名は {chatSystem.hostName}です。");

            while (true)
            {
                Console.Write("名前を入力してください。:");
                user_name = Console.ReadLine();

                if (user_name != "")
                {
                    user_name += ":";
                    break;
                }
                else
                {
                    Console.WriteLine("名前が検出できませんでした。もう一度入力してください。\n");
                }

            }

            connectMode = SelectMode();
            InGame();

        }

        static ChatSystem.ConnectMode SelectMode()
        {
            ChatSystem.ConnectMode connectMode = ChatSystem.ConnectMode.host;

            while (true)
            {
                Console.Write("モードを選択してください。[ 0か1を入力 ]\n{ 0 : Host , 1 : Client } : ");
                int select = int.Parse(Console.ReadLine());

                switch (select)
                {
                    case 0:

                        //Host
                        Console.WriteLine("ホストモードで起動します。");

                        isBot = SelectMode_Bot();
                        InitializeHost();
                        connectMode = ChatSystem.ConnectMode.host;
                        break;

                    case 1:

                        //Client
                        Console.WriteLine("クライアントモードで起動します。");
                        InitializeClient();
                        connectMode = ChatSystem.ConnectMode.client;
                        break;

                    default:

                        Console.WriteLine("入力が未定義でした。もう一度入力してください。\n");
                        break;
                }

                if (select == 0 || select == 1)
                {
                    break;
                }
            }

            return connectMode;
        }

        static bool SelectMode_Bot()
        {
            //ボットモード [ ON / OFF ]
            while (true)
            {
                Console.Write("ホストをbotにしますか? [ 0か1を入力 ]\n{ 0 : No , 1 : Yes } :");
                int select = int.Parse(Console.ReadLine());

                switch (select)
                {
                    case 0:

                        //No
                        Console.WriteLine("通常モードで起動します。\n");
                        return false;

                    case 1:

                        //Yes
                        Console.WriteLine("ボットモードで起動します。\n");
                        return true;

                    default:

                        Console.WriteLine("入力が未定義でした。もう一度入力してください。\n");
                        break;
                }
            }
        }

        static void InitializeHost()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(chatSystem.hostName);

            foreach (var addresslist in ipHostInfo.AddressList)
            {
                Console.WriteLine($"自分のアドレスが見つかりました:{addresslist.ToString()}");
            }

            int address_Select = 0;
            if (isBot)
            {
                address_Select = ipHostInfo.AddressList.Length - 1;
            }
            else
            {
                while (true)
                {
                    Console.Write($"\n公開するアドレスを選択してください。(0 から {ipHostInfo.AddressList.Length - 1}):");

                    address_Select = int.Parse(Console.ReadLine());

                    if (address_Select >= 0 && address_Select <= ipHostInfo.AddressList.Length - 1)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("例外が入力されました。もう一度入力してください。");
                    }
                }
            }

            Console.WriteLine("クライアント接続待ち…");

            IPAddress ipAddress = ipHostInfo.AddressList[ipHostInfo.AddressList.Length - 1];
            ChatSystem.EResult re = chatSystem.InitializeHost(ipAddress, portNo);
            
            Console.WriteLine("\n\n\n");//改行

            if (re != ChatSystem.EResult.success)
            {
                Console.WriteLine($"初期化に失敗しました。\nエラー内容 = {re.ToString()}");
            }

        }

        static void InitializeClient()
        {
            Console.Write("接続するIPアドレスを入力してください。:");
            var ipAddress = IPAddress.Parse(Console.ReadLine());
            ChatSystem.EResult re = chatSystem.InitializeClient(ipAddress, 11000);

            if (re == ChatSystem.EResult.success)
            {
                Console.WriteLine($"接続されたホスト。:{ipAddress.ToString()} \n\n\n");
            }
            else
            {
                Console.WriteLine($"ホストへの接続に失敗しました。\nエラー内容 ={chatSystem.resultMessage}");
            }
        }

        static void InGame()
        {
            ChatSystem.Buffer buffer = new ChatSystem.Buffer(maxLength);
            bool isTurn = true;

            Console.WriteLine("\nゲーム開始\n");
            string inputSt = "";
            string received = "";

            while (true)
            {
                if (!isTurn)
                {
                    // 受信
                    buffer = new ChatSystem.Buffer(maxLength);
                    ChatSystem.EResult re = chatSystem.Receive(buffer);

                    if (re == ChatSystem.EResult.success)
                    {
                        received = Encoding.UTF8.GetString(buffer.content).Replace(EOF, "");
                        int l = received.Length;

                        if (received[0] != '\0')
                        {
                            //正常にメッセージを受信
                            Console.WriteLine($"{received}");
                        }
                        else
                        {
                            //正常に終了を受信
                            Console.WriteLine("相手から終了を受信");
                            break;
                        }
                    }
                    else
                    {
                        //受信エラー
                        Console.WriteLine($"受信エラー：{chatSystem.resultMessage} ");
                        break;
                    }

                    //受信できる = 自分は入力済み。ここでジャッチ
                    Console.WriteLine("\n" + Game_Judge(inputSt, received) + "\n");
                }
                else
                {
                    //送信
                    inputSt = Hand_Select();

                    if (inputSt.Length > maxLength)
                    {
                        inputSt = inputSt.Substring(0, maxLength - EOF.Length);
                    }

                    Console.WriteLine(user_name + inputSt);

                    string input = user_name + inputSt + EOF;

                    if (inputSt == "\0")
                    {
                        input = inputSt;
                    }

                    buffer.content = Encoding.UTF8.GetBytes(input);
                    buffer.length = buffer.content.Length;
                    ChatSystem.EResult re = chatSystem.Send(buffer);

                    if (re != ChatSystem.EResult.success)
                    {
                        Console.WriteLine($"送信エラー：{re.ToString()} エラーコード : {chatSystem.resultMessage}");
                        break;
                    }

                    if (inputSt == "\0")
                    {
                        break;
                    }

                }

                isTurn = !isTurn;

            }

            chatSystem.ShutDownColse();
        }

        static string Hand_Select()
        {
            string select = "";
            while (true)
            {
                Console.Write("出す手を選択してください。[0から2を入力,空白は終了]\n{ 0：グー , 1：チョキ , 2：パー }:");

                if (isBot)
                {
                    select = rand.Next(0,3).ToString();
                    Console.WriteLine(select);
                }
                else
                {
                    select = Console.ReadLine();
                }

                if (select == "0")
                {
                    return "グー";
                }
                else if(select == "1")
                {
                    return "チョキ";
                }
                else if(select == "2")
                {
                    return "パー";
                }
                else
                {
                    return "\0";
                }

            }
        }

        static string Game_Judge(string my, string other)
        {
            string other_ans = "";

            bool isAns = false;

            for (int y = 0; y < other.Length; y++)
            {
                if (isAns)
                {
                    other_ans += other[y];
                }
                else if (other[y] == ':')
                {
                    isAns = true;
                }
            }

            Console.WriteLine(other_ans + ":");

            if (my.Contains("グー") && other_ans.Contains("グー") || my.Contains("チョキ") && other_ans.Contains("チョキ") || my.Contains("パー") && other_ans.Contains("パー"))
            {
                return "あいこ!!";
            }
            else if (my.Contains("グー") && other_ans.Contains("パー") || my.Contains("チョキ") && other_ans.Contains("グー") || my.Contains("パー") && other_ans.Contains("チョキ"))
            {
                return "あなたの負け!!";
            }
            else if (my.Contains("グー") && other_ans.Contains("チョキ") || my.Contains("チョキ") && other_ans.Contains("パー") || my.Contains("パー") && other_ans.Contains("グー"))
            {
                return "あなたの勝ち!!";
            }

            return "";
        }

    }
}