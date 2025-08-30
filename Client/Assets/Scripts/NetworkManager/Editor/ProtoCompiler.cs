using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;

public class ProtoCompiler : EditorWindow
{
    private string protoSourcePath = @"E:\WORK\GIT\Project_Audition\Server\Protos";
    private string outputPath = @"E:\WORK\GIT\Project_Audition\Client\Assets\Scripts\NetworkManager\Generated";

    [MenuItem("Tools/Protobuf/Compile Proto Files")]
    public static void ShowWindow()
    {
        GetWindow<ProtoCompiler>("Proto Compiler");
    }

    void OnGUI()
    {
        GUILayout.Label("Proto Compiler Settings", EditorStyles.boldLabel);
        
        protoSourcePath = EditorGUILayout.TextField("Proto Files Path", protoSourcePath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        
        if(GUILayout.Button("Compile Proto Files"))
        {
            CompileProtoFiles();
        }
    }
    
    private void CompileProtoFiles()
    {
        if(!Directory.Exists(protoSourcePath))
        {
            EditorUtility.DisplayDialog("Error", $"Proto source path does not exist: {protoSourcePath}", "OK");
            return;
        }
        
        if(!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
            
        // Grpc.Tools 패키지의 protoc.exe 경로 지정
        string protocPath = Path.Combine(Application.dataPath, "../Packages/Grpc.Tools.2.72.0/tools/windows_x64/protoc.exe");
        string csharpPluginPath = Path.Combine(Application.dataPath, "../Packages/Grpc.Tools.2.72.0/tools/windows_x64/grpc_csharp_plugin.exe");
        
        // 파일이 존재하는지 확인
        if(!File.Exists(protocPath))
        {
            EditorUtility.DisplayDialog("Error", $"protoc.exe not found at: {protocPath}", "OK");
            return;
        }
        
        if(!File.Exists(csharpPluginPath))
        {
            EditorUtility.DisplayDialog("Error", $"grpc_csharp_plugin.exe not found at: {csharpPluginPath}", "OK");
            return;
        }
        
        int successCount = 0;
        int failCount = 0;
        
        foreach(string protoFile in Directory.GetFiles(protoSourcePath, "*.proto"))
        {
            string fileName = Path.GetFileName(protoFile);
            UnityEngine.Debug.Log($"Compiling {fileName}...");
            
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = protocPath;
            startInfo.Arguments = $"--proto_path=\"{protoSourcePath}\" --csharp_out=\"{outputPath}\" --grpc_out=\"{outputPath}\" --plugin=protoc-gen-grpc=\"{csharpPluginPath}\" \"{protoFile}\"";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            
            Process process = Process.Start(startInfo);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            if(process.ExitCode == 0)
            {
                UnityEngine.Debug.Log($"Successfully compiled {fileName}");
                successCount++;
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to compile {fileName}: {error}");
                failCount++;
            }
        }
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Compilation Complete", $"Successfully compiled {successCount} files.\nFailed to compile {failCount} files.\nOutput path: {outputPath}", "OK");
    }
}
