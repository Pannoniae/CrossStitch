using System.IO.Compression;
using Raylib_cs;

namespace CrossStitch;

public class FileIO {

    public static void toFile(string file, Grid grid) {
        using var stream = File.Open(file, FileMode.Create);
        using var deflateStream = new DeflateStream(stream, CompressionMode.Compress);
        using var writer = new BinaryWriter(deflateStream);

        writer.Write(grid.width);
        writer.Write(grid.height);
        for (int i = 0; i < grid.width; i++) {
            for (int j = 0; j < grid.height; j++) {
                var e = grid.squares[i, j];
                writer.Write(e.type);
                writer.Write(repr(e.colour));
            }
        }
    }

    private static byte[] repr(Color colour) {
        return [colour.R, colour.B, colour.G, colour.A];
    }

    private static Color colour(byte[] bytes) {
        return new Color(bytes[0], bytes[1], bytes[2], bytes[3]);
    }

    public static Grid fromFile(string file) {
        var grid = new Grid();
        using var stream = File.Open(file, FileMode.Open);
        using var deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
        using var reader = new BinaryReader(deflateStream);
        grid.width = reader.ReadInt32();
        grid.height = reader.ReadInt32();
        for (int i = 0; i < grid.width; i++) {
            for (int j = 0; j < grid.height; j++) {
                var e = grid.squares[i, j];
                e.type = reader.ReadString();
                e.colour = colour(reader.ReadBytes(4));
            }
        }

        return grid;
    }
}