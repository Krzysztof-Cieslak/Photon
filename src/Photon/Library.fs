namespace Photon

module Core =
    open FSharp.Formatting
    open FSharp.MetadataFormat

    type DocInfo<'a> = {
        ParentName: string
        NamespaceName: string
        Info: 'a
    }





    let getDocs (fileName: string) =
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

        allModules, allTypes



///Some comment on the module
module Say =

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <example>
    /// <code>
    /// let v = hello "Chris"
    /// v = "Hello Chris"
    /// </code>
    /// </example>
    let hello name =
        printfn "Hello %s" name

    ///Some comment on the type
    type SampleType = {
        ///Some more comments
        A: string
        ///And more comments
        B: int

    }
