// tink.fs —— tink data-flow node frame protocol (universal, language-agnostic).
//
// Frame = [len u32 BE][payload][crc u32 BE]; crc = CRC32-IEEE (0xEDB88320).
// Mirrors std/tink.tie (tie standard library) and the other-language tink
// libraries; pure functions over byte[], IO (stdin/stdout) left to the
// caller. F# (.NET), no external dependencies.
//
//   let frame = Tink.frameEncode [| 1uy; 2uy; 3uy |]
//   let got = Tink.frameNext frame 0   // (byte[] * int) option

/// F# implementation of the tink data-flow node frame protocol.
module Tink
    let private be32 (b: byte[]) (off: int) : uint32 =
        let n = int b[off + 3] ||| (int b[off + 2] <<< 8) ||| (int b[off + 1] <<< 16) ||| (int b[off] <<< 24)
        uint32 n

    /// CRC32-IEEE over a byte array (bit-loop, no table; matches .NET CRC32).
    /// Check vector: crc32(ascii"123456789") = 0xCBF43926u.
    let crc32 (data: byte[]) : uint32 =
        let mutable crc = 0xFFFFFFFFu
        for b in data do
            crc <- crc ^^^ uint32 b
            for _ in 1..8 do
                crc <- if (crc &&& 1u) <> 0u then (crc >>> 1) ^^^ 0xEDB88320u else crc >>> 1
        (crc ^^^ 0xFFFFFFFFu) &&& 0xFFFFFFFFu

    /// Encode a payload into a full frame: [len u32 BE][payload][crc u32 BE].
    let frameEncode (payload: byte[]) : byte[] =
        let n = payload.Length
        let out = Array.zeroCreate (n + 8)
        out[0] <- byte (n >>> 24)
        out[1] <- byte (n >>> 16)
        out[2] <- byte (n >>> 8)
        out[3] <- byte n
        System.Array.Copy(payload, 0, out, 4, n)
        let c = crc32 payload
        out[n + 4] <- byte (int64 c >>> 24 &&& 0xFFL)
        out[n + 5] <- byte (int64 c >>> 16 &&& 0xFFL)
        out[n + 6] <- byte (int64 c >>> 8 &&& 0xFFL)
        out[n + 7] <- byte (int64 c &&& 0xFFL)
        out

    /// Result of parsing a frame: the payload (a copy) and the position after it.
    let (|Payload|) (t: byte[] * int) = t

    /// Parse one frame at pos (verifies CRC). Returns (payload, next) on
    /// success or None on out-of-bounds / CRC mismatch.
    let frameNext (bytes: byte[]) (pos: int) : (byte[] * int) option =
        if pos < 0 || bytes.Length < pos + 8 then None
        else
            let n = int (be32 bytes pos)
            let e = pos + 8 + n
            if bytes.Length < e then None
            else
                let payload = Array.sub bytes (pos + 4) n
                let want = be32 bytes (e - 4)
                if crc32 payload <> want then None else Some (payload, e)

    /// Skip one frame at pos without copying or verifying (zero-copy).
    /// Returns the position after the frame, or None on out-of-bounds.
    let frameSkip (bytes: byte[]) (pos: int) : int option =
        if pos < 0 || bytes.Length < pos + 8 then None
        else
            let n = int (be32 bytes pos)
            let e = pos + 8 + n
            if bytes.Length < e then None else Some e