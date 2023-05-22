using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NTPClient
{
     class Program
     {
          static void Main()
          {
               Console.OutputEncoding = Encoding.UTF8;

               bool continueProgram = true;

               while (continueProgram)
               {
                    Console.WriteLine("Introduceți zona geografică (GMT+X sau GMT-X):");
                    string input = Console.ReadLine();

                    // Eliminăm prefixul "GMT" și convertim cifra la un număr întreg
                    int timeZoneOffset;
                    if (input.StartsWith("GMT+"))
                    {
                         timeZoneOffset = int.Parse(input.Substring(4));
                    }
                    else if (input.StartsWith("GMT-"))
                    {
                         timeZoneOffset = -int.Parse(input.Substring(4));
                    }
                    else
                    {
                         Console.WriteLine("Formatul introdus nu este valid. Exemplu: GMT+2 sau GMT-5");
                         continue;
                    }

                    // Verificăm dacă offsetul introdus este în intervalul corect (-11 până la 11)
                    if (timeZoneOffset < -11 || timeZoneOffset > 11)
                    {
                         Console.WriteLine("Offsetul trebuie să fie între -11 și 11.");
                         continue;
                    }

                    // Obținem informațiile despre zona geografică bazată pe offset
                    TimeZoneInfo timeZone;
                    try
                    {
                         timeZone = TimeZoneInfo.CreateCustomTimeZone("CustomTimeZone", TimeSpan.FromHours(timeZoneOffset), "Custom Time Zone", "Custom Time Zone");
                    }
                    catch (Exception)
                    {
                         Console.WriteLine("Offsetul introdus nu este valid.");
                         continue;
                    }

                    // Obținem ora exactă pentru zona geografică specificată
                    DateTime localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);

                    Console.WriteLine($"Data și ora exactă în zona geografică {input} este: {localTime}");

                    // Inițializarea unui client NTP cu adresa "pool.ntp.org"
                    NtpTimeClient client = new NtpTimeClient("pool.ntp.org");

                    // Obținem timpul de la serverul NTP
                    try
                    {
                         DateTime ntpTime = client.GetNetworkTime();

                         Console.WriteLine($"Data și ora exactă bazată pe serverul NTP {client.Server} este: {ntpTime}");
                    }
                    catch (Exception)
                    {
                         Console.WriteLine("Nu s-a putut obține timpul de la serverul NTP.");
                    }

                    Console.WriteLine("Doriți să continuați? (Da/Nu)");
                    string continueInput = Console.ReadLine();

                    continueProgram = (continueInput.ToLower() == "da");
               }
          }
     }

     class NtpTimeClient
     {
          private string server;

          public string Server
          {
               get { return server; }
          }

          public NtpTimeClient(string server)
          {
               this.server = server;
          }

          public DateTime GetNetworkTime()
          {
               const int NtpDataLength = 48; // Lungimea datelor pentru protocolul NTP
               byte[] ntpData = new byte[NtpDataLength];
               ntpData[0] = 0x1B; // Indicator de protocol pentru cererea de timp NTP

               IPAddress[] addresses = Dns.GetHostAddresses(server);
               IPEndPoint ep = new IPEndPoint(addresses[0], 123); // Portul 123 este folosit pentru protocolul NTP

               using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
               {
                    socket.Connect(ep);

                    socket.ReceiveTimeout = 3000; // Timpul maxim de așteptare pentru răspunsul serverului NTP

                    socket.Send(ntpData);
                    socket.Receive(ntpData);
               }

               const byte serverReplyTimeOffset = 40; // Offset-ul de timp în pachetul de răspuns NTP
               ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTimeOffset);
               ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTimeOffset + 4);
               intPart = SwapEndianness(intPart);
               fractPart = SwapEndianness(fractPart);
               ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

               DateTime ntpTime = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);

               TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(ntpTime);

               return ntpTime + offset;
          }

          private static uint SwapEndianness(ulong x)
          {
               return (uint)(((x & 0x000000ff) << 24) +
                             ((x & 0x0000ff00) << 8) +
                             ((x & 0x00ff0000) >> 8) +
                             ((x & 0xff000000) >> 24));
          }
     }
}
