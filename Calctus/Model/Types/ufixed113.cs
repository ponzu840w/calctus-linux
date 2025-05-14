using System;
using System.Text;

namespace Shapoco.Calctus.Model.Types {
    struct ufixed113 {
        private const int N = 5;
        public const int NumBits = 113;

        // 値の内部表現
        private readonly uint _w0, _w1, _w2, _w3, _w4;

        public static readonly ufixed113 Zero = new ufixed113(0u, 0u, 0u, 0u, 0u);
        public static readonly ufixed113 One = new ufixed113(1u, 0u, 0u, 0u, 0u);

        public ufixed113(uint[] a, int offset = 0)
        {
            if (a == null || a.Length - offset < N) throw new ArgumentException();
            _w0 = a[offset + 0];
            _w1 = a[offset + 1];
            _w2 = a[offset + 2];
            _w3 = a[offset + 3];
            _w4 = a[offset + 4];
        }

        public ufixed113(uint e, uint d, uint c, uint b, uint a)
        {
            _w0 = a;
            _w1 = b;
            _w2 = c;
            _w3 = d;
            _w4 = e;
        }

        public uint Msb => _w4;

        public ulong Lower64Bits =>
            (ulong)_w0 | ((ulong)_w1 << 28) | ((ulong)_w2 << 56);

        public override bool Equals(object obj) {
            if (obj is ufixed113 objB) {
                return this == objB;
            }
            else {
                return false;
            }
        }

        public override int GetHashCode() => (int)(_w0 ^ _w1 ^ _w2 ^ _w3 ^ _w4);

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Convert.ToString(_w4, 16));
            uint[] parts = { _w3, _w2, _w1, _w0 };
            foreach (var p in parts)
            {
                sb.Append(' ');
                var hex = Convert.ToString(p, 16);
                sb.Append(hex.PadLeft(7, '0'));
            }
            return sb.ToString();
        }

        public ufixed113 Add(ufixed113 b, out uint carryOut)
        {
            ulong tmp;
            tmp =  _w0 + b._w0; uint r0 = (uint)(tmp & 0xFFFFFFFu); tmp >>= 28;
            tmp += _w1 + b._w1; uint r1 = (uint)(tmp & 0xFFFFFFFu); tmp >>= 28;
            tmp += _w2 + b._w2; uint r2 = (uint)(tmp & 0xFFFFFFFu); tmp >>= 28;
            tmp += _w3 + b._w3; uint r3 = (uint)(tmp & 0xFFFFFFFu); tmp >>= 28;
            tmp += _w4 + b._w4; uint r4 = (uint)(tmp & 0xFFFFFFFu);
            carryOut = (uint)((r4 >> 1) & 1u);
            r4 &= 1u;
            return new ufixed113(r4, r3, r2, r1, r0);
        }

        public ufixed113 Sub(ufixed113 b, out uint carryOut) {
            carryOut = this < b ? 1u : 0u;
            return Add(-b, out _);
        }

        public ufixed113 Mul(ufixed113 b, out uint carryOut)
        {
            const uint MASK = (1u << 28) - 1;

            // 1) フィールドをローカルに展開
            uint a0 = _w0, a1 = _w1, a2 = _w2, a3 = _w3, a4 = _w4;
            uint b0 = b._w0, b1 = b._w1, b2 = b._w2, b3 = b._w3, b4 = b._w4;

            // 2) 積和を蓄える ulong 配列（長さ = 2*N = 10）
            var accum = new ulong[2 * N];

            // 畳み込み演算（手動アンローリング）
            accum[0] = (ulong)a0 * b0;
            accum[1] = (ulong)a0 * b1 + (ulong)a1 * b0;
            accum[2] = (ulong)a0 * b2 + (ulong)a1 * b1 + (ulong)a2 * b0;
            accum[3] = (ulong)a0 * b3 + (ulong)a1 * b2 + (ulong)a2 * b1 + (ulong)a3 * b0;
            accum[4] = (ulong)a0 * b4 + (ulong)a1 * b3 + (ulong)a2 * b2 + (ulong)a3 * b1 + (ulong)a4 * b0;
            accum[5] = (ulong)a1 * b4 + (ulong)a2 * b3 + (ulong)a3 * b2 + (ulong)a4 * b1;
            accum[6] = (ulong)a2 * b4 + (ulong)a3 * b3 + (ulong)a4 * b2;
            accum[7] = (ulong)a3 * b4 + (ulong)a4 * b3;
            accum[8] = (ulong)a4 * b4;
            // accum[9] はキャリー伝播用に残しておく

            // 3) 正規化ループ：28 ビットごとにキャリーを次へ回す
            ulong c = 0;
            for (int i = 0; i < 2 * N; i++)
            {
                accum[i] += c;
                c = accum[i] >> 28;
                accum[i] &= MASK;
            }

            // 4) 結果の下位 5 ワードを取り出し
            uint r0 = (uint)accum[4];
            uint r1 = (uint)accum[5];
            uint r2 = (uint)accum[6];
            uint r3 = (uint)accum[7];
            uint r4 = (uint)accum[8];

            // 5) 最上位ビットをキャリー出力に、残りをフィールドにマスク
            carryOut = (r4 >> 1) & 1u;
            r4 &= 1u;

            // 6) 新しい ufixed113 を返す
            return new ufixed113(r4, r3, r2, r1, r0);
        }

        public ufixed113 Div(ufixed113 b, out ufixed113 q) {
            var a = this;
            if (b == Zero) throw new DivideByZeroException();
            a = a.Align(out int aShift);
            b = b.Align(out int bShift);
            int shiftRight = aShift - bShift;

            q = Zero;
            for (int i = 0; i < NumBits; i++) {
                if (a >= b) {
                    a -= b;
                    q = q.SingleShiftLeft(1u);
                }
                else {
                    q = q.SingleShiftLeft(0u);
                }
                a <<= 1;
            }

            if (shiftRight >= 0) {
                q = q.LogicShiftRight(shiftRight);
            }
            else {
                q <<= shiftRight;
            }

            return q;
        }

        public ufixed113 Align(out int shift) {
            var a = this;
            shift = 0;
            if (a != Zero) {
                while (a.Msb == 0u) {
                    a <<= 1;
                    shift++;
                }
            }
            return a;
        }

        public ufixed113 ArithInvert(out uint carry) {
            return (~this).Add(new ufixed113(0u, 0u, 0u, 0u, 1u), out carry);
        }

        public ufixed113 SingleShiftLeft(uint carry)
        {
            const uint MASK = (1u << 28) - 1;

            // 元のワードをローカルに展開
            uint a0 = _w0, a1 = _w1, a2 = _w2, a3 = _w3, a4 = _w4;

            // ビット 27 を「次のキャリー」に取り出しつつ左シフト
            uint m0 = (a0 >> 27) & 1u;
            uint r0 = ((a0 << 1) & MASK) | carry;

            uint m1 = (a1 >> 27) & 1u;
            uint r1 = ((a1 << 1) & MASK) | m0;

            uint m2 = (a2 >> 27) & 1u;
            uint r2 = ((a2 << 1) & MASK) | m1;

            uint m3 = (a3 >> 27) & 1u;
            uint r3 = ((a3 << 1) & MASK) | m2;

            // 最後のワードは前段キャリー
            uint r4 = m3;

            return new ufixed113(r4, r3, r2, r1, r0);
        }

        public ufixed113 SingleShiftRight(uint carryIn)
        {
            const uint MASK = (1u << 28) - 1;

            uint a0 = _w0, a1 = _w1, a2 = _w2, a3 = _w3, a4 = _w4;

            // キャリーをワード間で更新しながら伝搬
            uint carry = carryIn;
            uint nextCarry = a4 & 1u;
            uint r4 = carry;

            carry = nextCarry;
            nextCarry = a3 & 1u;
            uint r3 = ((carry << 27) & MASK) | (a3 >> 1);

            carry = nextCarry;
            nextCarry = a2 & 1u;
            uint r2 = ((carry << 27) & MASK) | (a2 >> 1);

            carry = nextCarry;
            nextCarry = a1 & 1u;
            uint r1 = ((carry << 27) & MASK) | (a1 >> 1);

            carry = nextCarry;
            // 最後のワード
            uint r0 = ((carry << 27) & MASK) | (a0 >> 1);

            return new ufixed113(r4, r3, r2, r1, r0);
        }

        public ufixed113 LogicShiftLeft(int n) {
            if (n <= 0) throw new ArgumentException();
            var a = this;
            for (int i = 0; i < n; i++) {
                a = a.SingleShiftLeft(0u);
            }
            return a;
        }

        public ufixed113 LogicShiftRight(int n) {
            if (n < 0) throw new ArgumentException();
            var a = this;
            for (int i = 0; i < n; i++) {
                a = a.SingleShiftRight(0u);
            }
            return a;
        }

        public ufixed113 ArithShiftRight(int n) {
            if (n < 0) throw new ArgumentException();
            uint msb = Msb;
            var a = this;
            for (int i = 0; i < n; i++) {
                a = a.SingleShiftRight(msb);
            }
            return a;
        }
        public ufixed113 TruncateRight(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (n >= NumBits) return Zero;

            int blocks = n / 28;
            int bits = n % 28;
            uint mask = bits == 0 ? 0xffffffffu : ~((1u << bits) - 1u);

            uint r0 = _w0, r1 = _w1, r2 = _w2, r3 = _w3, r4 = _w4;
            switch (blocks)
            {
                case 4:
                    r0 = 0; r1 = 0; r2 = 0; r3 = 0;
                    r4 &= mask;
                    break;
                case 3:
                    r0 = 0; r1 = 0; r2 = 0;
                    r3 &= mask;
                    break;
                case 2:
                    r0 = 0; r1 = 0;
                    r2 &= mask;
                    break;
                case 1:
                    r0 = 0;
                    r1 &= mask;
                    break;
                case 0:
                    r0 &= mask;
                    break;
            }
            return new ufixed113(r4, r3, r2, r1, r0);
        }

        public static implicit operator ufixed113(int i) {
            if (i == 0) return Zero;
            if (i == 1) return One;
            throw new OverflowException();
        }

        public static implicit operator ufixed113(double d) {
            if (d < 0 || d >= 2) throw new OverflowException();
            ufixed113 ret = Zero;
            for (int i = 0; i < NumBits; i++) {
                var tmp = (uint)Math.Floor(d);
                ret = ret.SingleShiftLeft(tmp);
                d -= tmp;
                d *= 2;
            }
            return ret;
        }

        public static ufixed113 operator ~(ufixed113 a)
        {
            const uint MASK = (1u << 28) - 1;
            // 下位 4 ワードは 28 ビット有効、最上位ワードは 1 ビットだけ
            uint r0 = (~a._w0) & MASK;
            uint r1 = (~a._w1) & MASK;
            uint r2 = (~a._w2) & MASK;
            uint r3 = (~a._w3) & MASK;
            uint r4 = (~a._w4) & 1u;
            return new ufixed113(r4, r3, r2, r1, r0);
        }

        public static ufixed113 operator -(ufixed113 a) => a.ArithInvert(out _);

        public static ufixed113 operator +(ufixed113 a, ufixed113 b) => a.Add(b, out _);
        public static ufixed113 operator -(ufixed113 a, ufixed113 b) => a.Sub(b, out _);
        public static ufixed113 operator *(ufixed113 a, ufixed113 b) => a.Mul(b, out _);
        public static ufixed113 operator /(ufixed113 a, ufixed113 b) { a.Div(b, out ufixed113 q); return q; }

        public static ufixed113 operator <<(ufixed113 a, int n) => a.LogicShiftLeft(n);
        public static ufixed113 operator >>(ufixed113 a, int n) => a.ArithShiftRight(n);

        public static bool operator ==(ufixed113 a, ufixed113 b)
          => a._w0 == b._w0
          && a._w1 == b._w1
          && a._w2 == b._w2
          && a._w3 == b._w3
          && a._w4 == b._w4;

        public static bool operator !=(ufixed113 a, ufixed113 b) => !(a == b);

        public static bool operator >(ufixed113 a, ufixed113 b)
        {
            if (a._w4 != b._w4) return a._w4 > b._w4;
            if (a._w3 != b._w3) return a._w3 > b._w3;
            if (a._w2 != b._w2) return a._w2 > b._w2;
            if (a._w1 != b._w1) return a._w1 > b._w1;
            return a._w0 > b._w0;
        }

        public static bool operator <(ufixed113 a, ufixed113 b) => b > a;
        public static bool operator >=(ufixed113 a, ufixed113 b) => (a > b) || (a == b);
        public static bool operator <=(ufixed113 a, ufixed113 b) => (a < b) || (a == b);

        public static void Test() {
            AssertAdd(0.5, 0.5, 1, 0u);
            AssertAdd(1, 1, 0, 1u);
            AssertSub(1, 1, 0, 0u);
        }

        public static void AssertAdd(double a, double b, double q, uint carry) {
            Assert.Equal("[" + nameof(ufixed113) + "] " + a + "+" + b, ((ufixed113)a).Add((ufixed113)b, out uint c), (ufixed113)q);
            Assert.Equal("[" + nameof(ufixed113) + "] carry(" + a + "+" + b + ")", carry, c);
        }

        public static void AssertSub(double a, double b, double q, uint carry) {
            Assert.Equal("[" + nameof(ufixed113) + "] " + a + "-" + b, ((ufixed113)a).Sub((ufixed113)b, out uint c), (ufixed113)q);
            Assert.Equal("[" + nameof(ufixed113) + "] carry(" + a + "-" + b + ")", carry, c);
        }

    }
}
