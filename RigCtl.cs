// Client minimo per il protocollo RigCtld (Hamlib): comandi di testo su TCP, una riga per comando.
// Riferimento: "F <Hz>" imposta la frequenza, "f" la legge; le risposte ai comandi "set" sono "RPRT <codice>".
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace DXSummitViewer
{
    public static class RigCtl
    {
        public const int DefaultPort = 4532;
        const int TimeoutMs = 2000;

        // Invia un comando e restituisce la prima riga di risposta, o null se il server non risponde entro il timeout.
        // Solleva un'eccezione se la connessione non riesce.
        public static string Send(string host, int port, string command)
        {
            using (var client = new TcpClient())
            {
                var ar = client.BeginConnect(host, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeoutMs))
                    throw new TimeoutException("nessuna risposta da " + host + ":" + port);
                client.EndConnect(ar);

                var stream = client.GetStream();
                stream.ReadTimeout = TimeoutMs;
                var bytes = Encoding.ASCII.GetBytes(command + "\n");
                stream.Write(bytes, 0, bytes.Length);

                var line = new StringBuilder();
                try
                {
                    int b;
                    while ((b = stream.ReadByte()) >= 0 && b != '\n')
                        if (b != '\r') line.Append((char)b);
                }
                catch (IOException)
                {
                    if (line.Length == 0) return null; // timeout: comando inviato ma nessuna risposta
                }
                return line.ToString();
            }
        }

        // Imposta la frequenza (kHz). Restituisce null se riuscito, altrimenti il motivo del fallimento.
        public static string SetFrequency(string host, int port, double khz)
        {
            long hz = (long)Math.Round(khz * 1000);
            string resp = Send(host, port, "F " + hz);
            if (resp == null || resp.Trim() == "RPRT 0") return null;
            if (resp.StartsWith("RPRT")) return "la radio ha rifiutato il comando (" + resp.Trim() + ")";
            return null; // risposta non standard: il comando è comunque stato consegnato
        }

        // Modo Hamlib da impostare per uno spot, o null per lasciare il modo della radio invariato.
        // SSB: LSB su 160, 80 e 40 m; USB altrove (60 m compresi, per convenzione).
        public static string HamlibMode(string spotMode, double khz)
        {
            switch (spotMode)
            {
                case "SSB":
                    string band = HamUtil.BandOf(khz);
                    return band == "160m" || band == "80m" || band == "40m" ? "LSB" : "USB";
                case "CW": return "CW";
                case "FM": return "FM";
                case "AM": return "AM";
                case "RTTY": return "RTTY";
                case "SSTV": return "USB";
                case "FT8":
                case "FT4":
                case "PSK":
                case "Digi": return "PKTUSB"; // USB-Data; se la radio non lo supporta si ripiega su USB
                default: return null;
            }
        }

        // Imposta il modo (passband 0 = larghezza di banda predefinita della radio).
        // Restituisce null se riuscito, altrimenti il motivo del fallimento.
        public static string SetMode(string host, int port, string mode)
        {
            string resp = Send(host, port, "M " + mode + " 0");
            if (resp == null || resp.Trim() == "RPRT 0" || !resp.StartsWith("RPRT")) return null;
            return "modo " + mode + " non accettato (" + resp.Trim() + ")";
        }

        // Legge la frequenza attuale in Hz (usato da "Prova connessione")
        public static long? GetFrequency(string host, int port)
        {
            string resp = Send(host, port, "f");
            long hz;
            if (resp != null && long.TryParse(resp.Trim(), out hz)) return hz;
            return null;
        }
    }
}
