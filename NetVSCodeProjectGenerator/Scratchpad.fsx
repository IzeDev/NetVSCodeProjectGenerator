open System
open System.IO

let validateFilePath path =
    not <| String.IsNullOrWhiteSpace(path) &&
    File.Exists(path) &&
    File.GetAttributes(path) = FileAttributes.Directory

