// Learn more about F# at http://fsharp.org

open System

[<EntryPoint>]
let main argv =
    let path = System.IO.Path.Combine("src", "Photon.Cli", "bin", "Debug", "netcoreapp3.1", "Photon.dll")
    let (modules, types) =  Photon.Core.getDocs path
    printfn "MODULES: %A" modules
    printfn "TYPES: %A" types
    0 // return an integer exit code
