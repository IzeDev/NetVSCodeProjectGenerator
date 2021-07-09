open System
open System.IO
open System.Diagnostics
open System.Xml

let baseCommands =
    [|
        "new sln -o ¤basedirectory¤/¤SolutionName¤"; 
        "new console -lang ¤lang¤ -o ¤basedirectory¤/¤SolutionName¤/¤SolutionName¤";
        "sln ¤basedirectory¤/¤SolutionName¤/¤SolutionName¤.sln add ¤basedirectory¤/¤SolutionName¤/¤SolutionName¤/¤SolutionName¤.¤langFileEnding¤";
    |]

let buildJson = "
    {
        // See https://go.microsoft.com/fwlink/?LinkId=733558
        // for the documentation about the tasks.json format
        \"version\": \"2.0.0\",
        \"tasks\": [
            {
                \"label\": \"restore solution\",
                \"command\": \"dotnet\",
                \"type\": \"process\",
                \"args\": [
                \"restore\",
                // Ask dotnet build to generate full paths for file names.
                \"/property:GenerateFullPaths=true\"
                ],
                \"problemMatcher\": \"$msCompile\"
            },
            {
                \"label\": \"build\",
                \"command\": \"dotnet\",
                \"type\": \"shell\",
                \"args\": [
                    \"build\",
                    // Ask dotnet build to generate full paths for file names.
                    \"/property:GenerateFullPaths=true\",
                    // Do not generate summary otherwise it leads to duplicate errors in Problems panel
                    \"/consoleloggerparameters:NoSummary\"
                ],
                \"group\": \"build\",
                \"presentation\": {
                    \"reveal\": \"silent\"
                },
                \"problemMatcher\": \"$msCompile\",
                \"dependsOn\": [
                    \"restore solution\"
                    ]
            }
        ]
    }"

let launchJson = "
    {
        \"version\": \"0.2.0\",
        \"configurations\": [
            {
                \"name\": \".NET Core Launch (console)\",
                \"type\": \"coreclr\",
                \"request\": \"launch\",
                \"preLaunchTask\": \"build\",
                \"program\": \"${workspaceFolder}/¤SolutionName¤/bin/Debug/¤targetFramework¤/¤SolutionName¤.dll\",
                \"args\": [],
                \"cwd\": \"${workspaceFolder}\",
                \"stopAtEntry\": false,
                \"console\": \"externalTerminal\"
            },
        ]
    }"

let rec getUserInput msg validator =
    printfn "%s " msg
    let input = Console.ReadLine()
    if validator input then
        input
    else
        getUserInput msg validator

[<EntryPoint>]
let main argv =
    let stringHasContent text =
        not <| String.IsNullOrWhiteSpace(text)
    let validateFilePath path =
        stringHasContent path &&
        Directory.Exists path

    let basedirectory =        
        getUserInput
            "Please enter the root/source folder for your programming projects."
            validateFilePath
    let solutionName =
        getUserInput
            "Please enter the name of your solution"
            (String.IsNullOrWhiteSpace >> not)
    let programmingLanguageInput =
        getUserInput
            (sprintf "Please pick desired programming language:%s1: F#%s2: C#%s3: Visual Basic"
                Environment.NewLine Environment.NewLine Environment.NewLine)            
            (fun x -> x = "1" || x = "2" || x = "3")
        
    let dotNetLanguage=
        match programmingLanguageInput with
        | "1" -> "F#"
        | "2" -> "C#"
        |_ -> "VB"

    let dotNetLanguageFileEnding=
        match programmingLanguageInput with
        | "1" -> "fsproj"
        | "2" -> "csproj"
        |_ -> "vbproj"

    for i in 0 .. baseCommands.Length - 1 do
        let command = ((Array.get baseCommands i).Replace("¤basedirectory¤", basedirectory).Replace("¤SolutionName¤", solutionName)).Replace("¤lang¤", dotNetLanguage).Replace("¤langFileEnding¤", dotNetLanguageFileEnding)
        printfn "dotnet %s" command
        let p = Process.Start("dotnet", command)
        p.WaitForExit()

    let targetFramework =
        let xml = File.ReadAllText(basedirectory + "/" + solutionName + "/" + solutionName + "/" + solutionName + "." + dotNetLanguageFileEnding)
        let doc = XmlDocument() in
            doc.LoadXml xml;
            doc.SelectNodes "/Project/PropertyGroup/TargetFramework/text()"
                |> Seq.cast<XmlNode>
                |> Seq.map (fun node -> node.Value)
                |> String.concat Environment.NewLine

    let launchJsonTest = launchJson.Replace("¤basedirectory¤", basedirectory).Replace("¤SolutionName¤", solutionName).Replace("¤targetFramework¤", targetFramework)
    Directory.CreateDirectory(basedirectory + "/" + solutionName + "/.vscode") |> ignore
    File.WriteAllText(basedirectory + "/" + solutionName + "/.vscode/launch.json", launchJsonTest)
    File.WriteAllText(basedirectory + "/" + solutionName + "/.vscode/tasks.json", buildJson)
    let p = new Process()
    p.StartInfo.UseShellExecute <- true
    p.StartInfo.FileName <- "code"
    p.StartInfo.Arguments <- basedirectory + "/" + solutionName
    p.Start() |> ignore
    0 // return an integer exit code
