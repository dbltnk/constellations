using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

public class GameLogic : MonoBehaviour {

    public int MinEmpty = 0;
    public int MaxEmpty = 2;
    public int MaxGreen = 10;
    public int MaxBlue = 10;
    public int Iterations = 10000;
    public int PrintEveryXFields = 100;

    const uint EMPTY = 0;
    const uint GREEN = 1;
    const uint BLUE = 2;
    const uint BOTH = 3;

    List<Card> Cards = new List<Card>();

    float CardFreqTotal;

    float PieceFreqEmpty;
    float PieceFreqGreen;
    float PieceFreqBlue;
    float PieceFreqTotal;

    class Card {
        public string Name;
        public List<uint> Masks = new List<uint>();
        public float Frequency;

        public Card(string name, int dX1, int dY1, uint firstPiece, int dX2, int dY2, uint secondPiece) {
            Name = name;
            Masks = CreateCardMasks(dX1, dY1, firstPiece, dX2, dY2, secondPiece);
            Frequency = 0;
        }

        List<uint> CreateCardMasks (int dX1, int dY1, uint firstPiece, int dX2, int dY2, uint secondPiece) {
            List<uint> masks = new List<uint>();
            for (int x = 0; x < 4; ++x) {
                for (int y = 0; y < 4; ++y) {
                    uint mask = 0;
                    bool ok = true;
                    ok = ok && SetPiece(ref mask, x + dX1, y + dY1, firstPiece);
                    ok = ok && SetPiece(ref mask, x + dX2, y + dY2, secondPiece);
                    if (ok) {
                        masks.Add(mask);
                    }
                }
            }
            return masks;
        }

        public override string ToString() {
            string s = $"{Name} ({Frequency}) \n\n";
            foreach (uint mask in Masks) s = string.Concat(s, RenderField(mask), "\n");
            return s;
        }
    }

    void Start () {
        Cards.Add(new Card("blue-blue-horizontal", 0, 0, BLUE, 1, 0, BLUE));
        Cards.Add(new Card("blue-blue-vertical", 0, 0, BLUE, 0, 1, BLUE));
        Cards.Add(new Card("blue-blue-diagonal-up", 0, 0, BLUE, -1, -1, BLUE));
        Cards.Add(new Card("blue-blue-diagonal-down", 0, 0, BLUE, 1, 1, BLUE));

        Cards.Add(new Card("green-green-horizontal", 0, 0, GREEN, 1, 0, GREEN));
        Cards.Add(new Card("green-green-vertical", 0, 0, GREEN, 0, 1, GREEN));
        Cards.Add(new Card("green-green-diagonal-up", 0, 0, GREEN, -1, -1, GREEN));
        Cards.Add(new Card("green-green-diagonal-down", 0, 0, GREEN, 1, 1, GREEN));

        Cards.Add(new Card("green-blue-horizontal", 0, 0, GREEN, 1, 0, BLUE));
        Cards.Add(new Card("green-blue-vertical", 0, 0, GREEN, 0, 1, BLUE));
        Cards.Add(new Card("green-blue-diagonal-up", 0, 0, GREEN, -1, -1, BLUE));
        Cards.Add(new Card("green-blue-diagonal-down", 0, 0, GREEN, 1, 1, BLUE));

        Cards.Add(new Card("blue-green-horizontal", 0, 0, BLUE, 1, 0, GREEN));
        Cards.Add(new Card("blue-green-vertical", 0, 0, BLUE, 0, 1, GREEN));
        Cards.Add(new Card("blue-green-diagonal-up", 0, 0, BLUE, -1, -1, GREEN));
        Cards.Add(new Card("blue-green-diagonal-down", 0, 0, BLUE, 1, 1, GREEN));

        foreach (Card card in Cards) print(card);

        Card[] cards = Cards.ToArray();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i < Iterations; ++i) {
            uint randomField = CreateRandomField(MinEmpty, MaxEmpty, MaxBlue, MaxGreen);
            if (i % PrintEveryXFields == 0) print(RenderField(randomField));

            foreach (Card card in cards) {
                foreach (uint mask in card.Masks) {
                    if ((randomField & mask) == mask) {
                        card.Frequency++;
                        CardFreqTotal++;
                    }
                }
            }
        }

        stopwatch.Stop();
        print(stopwatch.ElapsedMilliseconds);

        print("### Field Frequencies ###");
        print($"Empty: {PieceFreqEmpty} ({PieceFreqEmpty / PieceFreqTotal})");
        print($"Green: {PieceFreqGreen} ({PieceFreqGreen / PieceFreqTotal})");
        print($"Blue: {PieceFreqBlue} ({PieceFreqBlue / PieceFreqTotal})");

        print("### Card Frequencies ###");
        Cards = Cards.OrderByDescending(t => t.Frequency).ToList();
        foreach (Card card in Cards) {
            float perc = card.Frequency / CardFreqTotal * 100f;
            print($"{card.Name}:{card.Frequency} ({perc.ToString("F2")}%)");
        }
    }

    static int ShiftIndex (int x, int y) {
        return (y * 4 + x) * 2;
        // (0,0) -> 0
        // (1,0) -> 2
        // (2,0) -> 4
        // (0,1) -> 8
        // (3,3) -> 30  
    }

    static bool SetPiece (ref uint field, int x, int y, uint piece) {
        if (x < 0 || y < 0 || x >= 4 || y >= 4) return false;
        // piece in [0,1,2]

        // ~ is bitwise not (flips bits)
        // unsigned int clearMask = ~((unsigned int)both << shiftIndex(x,y));
        // field = (field & clearMask) | (piece << shiftIndex(x,y));

        field = field | (piece << ShiftIndex(x, y));
        return true;
    }

    static uint GetPiece (uint field, int x, int y) {
        return (field >> ShiftIndex(x, y)) & BOTH;  // 3 = 2 bits (11)
    }

    uint CreateRandomField (int minEmpties, int maxEmpties, int maxBlues, int maxGreens) {
        List<uint> pieces = new List<uint>();
        int empties = Random.Range(minEmpties, maxEmpties + 1);
        int blues = Random.Range(0, maxBlues + 1);
        int greens = Random.Range(0, maxGreens + 1);
        for (int i = 0; i < blues; ++i) {
            pieces.Add(BLUE);
        }
        for (int i = 0; i < greens; ++i) {
            pieces.Add(GREEN);
        }
        for (int i = 0; i < empties; ++i) {
            pieces.Add(EMPTY);
        }
        while (pieces.Count < 16) {
            // hacky way to make sure there are roughly as many blues as greens
            if (Random.Range(0f, 1f) <= 0.5f) {
                if (blues < maxBlues && blues <= greens) { pieces.Add(BLUE); blues++; }
                else if (greens < maxGreens && greens <= blues) { pieces.Add(GREEN); greens++; }
                else pieces.Add(EMPTY);
            }
            else {
                if (greens < maxGreens && greens <= blues) { pieces.Add(GREEN); greens++; }
                else if (blues < maxBlues && blues <= greens) { pieces.Add(BLUE); blues++; }
                else pieces.Add(EMPTY);
            }
        }
        Shuffle(pieces);
        uint field = 0;
        for (int i = 0; i < 16; ++i) {
            field = field | (pieces[i] << (i * 2));
        }

        for (int x = 0; x < 4; ++x) {
            for (int y = 0; y < 4; ++y) {
                uint piece = GetPiece(field, x, y);
                if (piece == EMPTY) PieceFreqEmpty++;
                if (piece == BLUE) PieceFreqBlue++;
                if (piece == GREEN) PieceFreqGreen++;
                PieceFreqTotal++;
            }
        }

        return field;
    }

    private static System.Random rng = new System.Random();

    public static void Shuffle<T> (IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    static string RenderField(uint field) {
        string s = "";
        for (int x = 0; x < 4; ++x) {
            for (int y = 0; y < 4; ++y) {
                s += GetPiece(field, x, y);
                if (y == 3) s += "\n";
            }
        }
        return(s);
    }
}
