namespace Photon
open System.IO

module Core =
    open FSharp.Formatting
    open FSharp.MetadataFormat

    type DocInfo<'a> = {
        ParentName: string
        NamespaceName: string
        Info: 'a
    }

    let getTests (fileName: string) =
        let props = ["project-name", "Photon"]

        let output = MetadataFormat.Generate(fileName, parameters = props, markDownComments = false, publicOnly = true)

        let allModules =
            let rec collectModules pn nn (m: Module) =
                [
                    yield { ParentName = pn; NamespaceName = nn; Info =  m}
                    yield! m.NestedModules |> List.collect (collectModules m.Name nn )
                ]
            output.AssemblyGroup.Namespaces
            |> List.collect (fun n ->
                n.Modules |> List.collect (fun m -> collectModules n.Name n.Name m)
            )

        let allTypes =
            [
                yield!
                    output.AssemblyGroup.Namespaces
                    |> List.collect (fun n ->
                        n.Types |> List.map (fun t -> {ParentName = n.Name; NamespaceName = n.Name; Info = t} )
                    )
                yield!
                    allModules
                    |> List.collect (fun n ->
                        n.Info.NestedTypes |> List.map (fun t -> {ParentName = n.Info.Name;  NamespaceName = n.NamespaceName; Info = t}) )
            ]

        let modulesFunctionsWithTest =
            allModules
            |> Seq.collect (fun m ->
                m.Info.AllMembers
                |> Seq.filter (fun n -> n.Comment.RawData |> Seq.exists (fun kv -> kv.Key = "test"))
                |> Seq.map (fun n ->
                    {ParentName = m.ParentName; NamespaceName = m.NamespaceName; Info = n }
                )
            )

        let typeMembersWithTest =
            allTypes
            |> Seq.collect (fun t ->
                t.Info.AllMembers
                |> Seq.filter (fun n -> n.Comment.RawData |> Seq.exists (fun kv -> kv.Key = "test"))
                |> Seq.map (fun n ->
                    {ParentName = t.ParentName; NamespaceName = t.NamespaceName; Info = n }
                )
            )

        [yield! modulesFunctionsWithTest; yield! typeMembersWithTest]

module EvaluatorHelpers =
    open FSharp.Reflection
    open System.Globalization
    open FSharp.Compiler.Interactive.Shell
    open System.Text

    let sbOut = StringBuilder()
    let sbErr = StringBuilder()
    let fsi () =
        let refs =
            ProjectSystem.FSIRefs.getRefs ()
            |> List.map (fun n -> sprintf "-r:%s" n)


        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)
        try
            let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
            let argv = [|
                yield! refs
                yield "--noframework"
                yield "/temp/fsi.exe";
                yield "--define:FORNAX"|]
            FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errStream)
        with
        | ex ->
            printfn "Error: %A" ex
            printfn "Inner: %A" ex.InnerException
            printfn "ErrorStream: %s" (errStream.ToString())
            raise ex

module Test =

    /// <summary>
    /// Let's have some sample summary
    /// </summary>
    /// <summary>
    /// Let's have some other summary
    /// </summary>
    /// <param name="name">Your name</param>
    /// <returns>String</returns>
    /// <example>
    /// let v = hello "Chris"
    /// v = "Hello Chris"
    /// </example>
    /// <test>
    /// let v = hello "Gien"
    /// v = "Hello Gien"
    /// </test>
    let hello name =
        sprintf "Hello %s" name

    /// <summary>
    /// Let's have some sample summary
    /// </summary>
    /// <test>
    /// let v = hello2 "Gien"
    /// v = "Hello Gien"
    /// </test>
    let hello2 name =
        sprintf "Hello %s" name

    /// <summary>
    /// Let's have some sample summary
    /// </summary>
    /// <test>
    /// let v = hello3 "Gien"
    /// v = "Hello Gien"
    /// </test>
    let hello3 name =
        sprintf "Hello %s" name

    let sampleNoTest t = ()

    type A = {x: int}
    with
        /// <summary>
        /// Let's have some sample summary
        /// </summary>
        /// <test>
        /// let a = {x = 12}
        /// let v = a.SomeMember()
        /// v = 43
        /// </test>
        member _.SomeMember () = 42