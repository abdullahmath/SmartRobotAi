using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Diagnostics;

public class VoicePythonControl : MonoBehaviour
{
    private KeywordRecognizer recognizer;
    private Process pythonProcess;

    void Start()
    {
        recognizer = new KeywordRecognizer(new[] { "hello smart", "thanks" });
        recognizer.OnPhraseRecognized += args => {
            if (args.text.ToLower() == "hello smart") StartPython();
            else if (args.text.ToLower() == "thanks") StopPython();
        };
        recognizer.Start();
    }

    void StartPython()
    {
        if (pythonProcess == null || pythonProcess.HasExited)
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = @"C:\Users\abdul\AppData\Local\Programs\Python\Python313\python.exe"; // ÊÚÏíá ÇáãÓÇÑ
            pythonProcess.StartInfo.Arguments = "\"C:\\Users\\abdul\\Desktop\\gem.py\"";
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.Start(); 
            UnityEngine.Debug.Log("API started");
        }
    }
    


    void StopPython()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess = null;
            UnityEngine.Debug.Log("API has stopped");
        }
    }

    void OnApplicationQuit() => StopPython();
}