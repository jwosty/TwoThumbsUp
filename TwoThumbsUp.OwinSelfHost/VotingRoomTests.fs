namespace TwoThumbsUp
open NUnit.Framework
open FsUnit

module Tests =
    [<Test>]
    let shouldAdd() = 2 + 3 |> should equal 5