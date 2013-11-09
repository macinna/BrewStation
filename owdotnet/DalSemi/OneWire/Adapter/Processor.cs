// TODO: Disposal

// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.IO;

namespace DalSemi.OneWire.Adapter
{
    public class Processor
    {

        private Process p;

        private StreamWriter sw;
        private StreamReader sr;
        private StreamReader err;


        public Processor(string ExecCmd)
        {

            p = new Process();
            ProcessStartInfo psI = new ProcessStartInfo(ExecCmd);
            psI.UseShellExecute = false; // to be able to redirect in, out and error
            psI.RedirectStandardInput = true;
            psI.RedirectStandardOutput = true;
            psI.RedirectStandardError = true;
            psI.CreateNoWindow = true; // do not make (the DOS-)window visible
            p.StartInfo = psI;
            if (!p.Start())
                throw new Exception("Cannot start " + ExecCmd);
            sw = p.StandardInput;
            sr = p.StandardOutput;
            err = p.StandardError;
            sw.AutoFlush = true;
            /*
            if (tbComm.Text != "")
                sw.WriteLine(tbComm.Text);
            else
                //execute default command
                sw.WriteLine("dir \\");
            sw.Close();
            textBox1.Text = sr.ReadToEnd();
            textBox1.Text += p.StandardError.ReadToEnd();
            */

        }

        public static Processor Exec(string ExecCmd)
        {
            return new Processor(ExecCmd);
        }

        public string ReadLine()
        {
            string res = sr.ReadLine();
            if (res == null)
                throw new Exception("Process bailed");
            return res;
        }

        public void Write(string s)
        {
            sw.Write(s);
        }

        public void Flush()
        {
            // sw.AutoFlush = already true... So sw.Flush() is not needed
            // sw.Flush();
        }

    }
}
