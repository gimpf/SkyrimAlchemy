module IO

open System.IO

let readLines (reader : TextReader) =
    Seq.unfold
        <| fun line ->
            if line = null then
                reader.Close()
                None
            else
                Some (line, reader.ReadLine())
        <| reader.ReadLine()

let fromFile path =
    let stream = File.OpenRead(path)
    new StreamReader(stream)

let toFile path =
    let stream = new FileStream(path, FileMode.Create, FileAccess.Write)
    new StreamWriter(stream)
