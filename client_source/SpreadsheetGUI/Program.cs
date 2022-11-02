using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using SSController;


//Code taken from Examples Repository CS3500
namespace SpreadsheetGUI
{
    /// <summary>
    /// Keeps track of how many top-level forms are running
    /// </summary>
    class SpreadsheetApplicationContext : ApplicationContext
    {
        // Number of open forms
        private int formCount = 0;

        // Singleton ApplicationContext
        private static SpreadsheetApplicationContext appContext;

        /// <summary>
        /// Private constructor for singleton pattern
        /// </summary>
        private SpreadsheetApplicationContext()
        {
        }

        /// <summary>
        /// Returns the one DemoApplicationContext.
        /// </summary>
        public static SpreadsheetApplicationContext getAppContext()
        {
            if (appContext == null)
            {
                appContext = new SpreadsheetApplicationContext();
            }
            return appContext;
        }

        /// <summary>
        /// Runs the form
        /// </summary>
        public void RunForm(Form form)
        {
            // One more form is running
            formCount++;

            // When this form closes, we want to find out
            form.FormClosed += (o, e) => { if (--formCount <= 0) ExitThread(); };

            // Run the form
            form.Show();
        }

    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application. 
        /// </summary>
        static void Main()
        {

            Controller controller = new Controller();
            controller.onFilesSent += new ControllerEventHandler(OnRecieveFiles);
            controller.onErrorOccured += new ControllerEventHandler(onError);

            string hostName = Interaction.InputBox("Enter a host name", "", "");
            if (hostName.Equals(""))
            {
                onError("Please enter a valid host.");
                return;
            }
            string port = Interaction.InputBox("Enter a port", "", "1100");
            if (!Int32.TryParse(port, out int portNum))
            {
                onError("Please enter a vaid port.");
                return;
            }
            string userName = "";
            do
            {
                userName = Interaction.InputBox("Enter a username", "name", "");
                if (userName.Equals(""))
                {
                    onError("Please enter a valid username.");
                }
            } while (userName.Equals(""));

            controller.sendConnectRequest(userName, hostName, portNum);

            //Wait for response from server
            Thread.Sleep(800);
            // After five seconds if we are not in the handshake we should disconnect
            if (!controller.inHanshake())
            {
                return;
            }
            // This ensures the spreadsheet stays open as long as we are connected to the server. 
            while (controller.getConnected()) 
            {
                Thread.Sleep(100);
            }
        }


        /// <summary>
        /// The method called when the server sends files and the controller has parsed said files.
        /// </summary>
        static void OnRecieveFiles(ControllerEventArgs e)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SpreadsheetApplicationContext appContext = SpreadsheetApplicationContext.getAppContext();

            Controller controller = e.getController();
            string inputFiles = "";
            foreach (string s in e.getFiles())
            {
                inputFiles += s;
            }

            string output = Interaction.InputBox("Current files: \n" + inputFiles, "Enter a Spreadsheet file", "");
            controller.setFileName(output);
            if (output != "")
            {
                Thread t = new Thread(() => openForm(controller, output, appContext));
                t.Start();
                controller.sendOpenSpreadsheetRequest(output);
            }
            else
            {
                controller.setConnected(false);
                onError("Must enter a valid spreadsheet name");
            }
        }

        static void onError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        static void onError(ControllerEventArgs e)
        {
            DialogResult error = MessageBox.Show(e.getErrorMessage(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        [STAThread]
        static void openForm(Controller controller, string output, SpreadsheetApplicationContext appContext) 
        {
            appContext.RunForm(new Form1(controller));
            Application.Run(appContext);
        }

    }
}
