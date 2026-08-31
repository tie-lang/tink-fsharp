// test_tink.fs —— unit tests for Tink.fs. Run: dotnet run
open System.Text

let mutable failures = 0

let check (cond: bool) (name: string) =
    if cond then printfn "[PASS] %s" name
    else
        failures <- failures + 1
        printfn "[FAIL] %s" name

// crc32 check vector
check (Tink.crc32 (Encoding.UTF8.GetBytes "123456789") = 0xCBF43926u) "crc32 vector"

// frame roundtrip
let p = [| 1uy; 2uy; 3uy |]
let frame = Tink.frameEncode p
check (frame.Length = p.Length + 8) "frame length"
match Tink.frameNext frame 0 with
| Some (payload, nxt) ->
    check (nxt = frame.Length) "frame next == length"
    check (payload = p) "frame payload roundtrip"
| None -> check false "frame present"

// empty frame roundtrip
match Tink.frameNext (Tink.frameEncode [||]) 0 with
| Some (payload, nxt) -> check (nxt = 8 && payload.Length = 0) "empty frame roundtrip"
| None -> check false "empty frame present"

// CRC tamper rejected
let ft = Tink.frameEncode p
ft[4] <- ft[4] + 1uy // tamper payload[0]
check (Tink.frameNext ft 0 |> Option.isNone) "crc tamper rejected"

// frameSkip matches length
check (Tink.frameSkip frame 0 = Some frame.Length) "frameSkip matches length"

// out of bounds
check (Tink.frameNext frame frame.Length |> Option.isNone) "frameNext out of bounds"
check (Tink.frameSkip frame frame.Length |> Option.isNone) "frameSkip out of bounds"
check (Tink.frameNext [||] 0 |> Option.isNone) "frameNext empty input"

if failures > 0 then
    printfn "%d checks FAILED" failures
    exit 1
printfn "all tests passed"

[<EntryPoint>]
let main _ = 0