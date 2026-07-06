using System;
using System.Windows.Forms;

namespace WindowsHelloScript___HelloIR;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Console.WriteLine("=================================");
        Console.WriteLine(" HelloIR Debug Build");
        Console.WriteLine("=================================");
        Console.WriteLine();

        ApplicationConfiguration.Initialize();

        Application.Run(new HiddenWindow());
    }
}