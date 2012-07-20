using System;
using System.Windows.Forms;
using org.lb.NLisp;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private readonly Lisp lisp;

        public Form1()
        {
            InitializeComponent();
            lisp = new Lisp();
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
                lisp.EvaluateScript(textBox2.Lines);
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
                    Print(lisp.ObjectToString(lisp.Evaluate(textBox3.Text)));
                    textBox3.Text = "";
                }
                catch (Exception ex)
                {
                    Print(ex.Message);
                }
            }
        }

        public void Print(string what)
        {
            textBox1.AppendText(what + "\r\n");
            textBox1.ScrollToCaret();
        }
    }
}
