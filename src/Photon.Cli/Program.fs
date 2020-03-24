// Learn more about F# at http://fsharp.org

open System
open Photon

[<EntryPoint>]
let main argv =
    let path = System.IO.Path.Combine("/Users/chris/Programming/Photon", "src", "Photon.Cli", "bin", "Debug", "netcoreapp3.1", "Photon.dll")
    let membersWithTest = Core.getTests path
    let tests =
        membersWithTest |> Seq.map (fun n ->
            let test =
                n.Info.Comment.RawData
                |> Seq.find (fun n -> n.Key = "test")
            let test = test.Value
            let test =
                test.Split('\n')
                |> Seq.map (fun n ->
                    let n = n.Trim()
                    sprintf "    %s" n)
                |> String.concat "\n"

            n, sprintf "\nlet %s_test () = \n  test(\"Test of %s\", fun () ->\n%s\n)" n.Info.Name n.Info.Name test
        )

    let testToPrint = tests |> Seq.map snd
    let namespaces =
        tests
        |> Seq.map (fun (n,_) -> sprintf "open %s" n.NamespaceName)
    let modules =
        tests
        |> Seq.map (fun (n,_) -> sprintf "open %s" n.ParentName)

    let opens = [yield! namespaces; yield! modules] |> Seq.distinct
    let runTestCalls =
        tests
        |> Seq.map (fun (n, _) -> sprintf "%s_test ()" n.Info.Name )
        |> String.concat "\n"

    let testFunction =
        """
let test (name: string, body: unit -> bool) =
  let result = body ()
  if result then
    printfn "%s - Passing" name
  else
    printfn "%s - Failed" name
"""

    let content =
        [ sprintf "#r \"%s\"" path
          ""
          yield! opens
          ""
          testFunction
          ""
          yield! testToPrint
          ""
          runTestCalls
        ]

    let fsi = EvaluatorHelpers.fsi ()
    fsi.EvalInteraction (content |> String.concat "\n")

    System.IO.File.WriteAllLines("test.fsx", content)
    0 // return an integer exit code
