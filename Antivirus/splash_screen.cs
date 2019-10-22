using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Antivirus
{
    public partial class frm_splsh : Form
    {
        public frm_splsh()
        {
            InitializeComponent();
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            progressBar1.Value = progressBar1.Value + 1;

            if (progressBar1.Value == 200)
            {
                timer1.Enabled = false;
                this.Close();
            }
        }

    
    }
}

