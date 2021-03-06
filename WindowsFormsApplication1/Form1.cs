﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using org.lb.NLisp;

namespace WindowsFormsApplication1
{
    public sealed partial class Form1 : Form
    {
        private readonly NLisp lisp;

        public Form1()
        {
            InitializeComponent();
            lisp = new NLisp();
            lisp.Print += Print;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5) RunScript();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RunScript();
        }

        private void RunScript()
        {
            try
            {
                Print("> run");
                Measure(() => lisp.Evaluate(string.Join("\n", textBox2.Lines)));
                textBox3.Focus();
            }
            catch (Exception ex)
            {
                Print(ex.Message);
            }
        }

        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    Print("> " + textBox3.Text);
                    Measure(() => { Print(lisp.Evaluate(textBox3.Text).ToString()); return 0; });
                    textBox3.Text = "";
                }
                catch (Exception ex)
                {
                    Print(ex.Message);
                }
            }
        }

        private void Print(string what)
        {
            textBox1.AppendText(what + "\r\n");
            textBox1.ScrollToCaret();
        }

        void Measure<T>(Func<T> f)
        {
            var s1 = new Stopwatch();
            s1.Start();
            f();
            s1.Stop();
            Print(s1.Elapsed.ToString());
        }
    }
}
