﻿// <copyright file="IccLutAToBTagDataEntry.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp
{
    using System;
    using System.Linq;
    using System.Numerics;

    /// <summary>
    /// This structure represents a color transform.
    /// </summary>
    internal sealed class IccLutAToBTagDataEntry : IccTagDataEntry, IEquatable<IccLutAToBTagDataEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IccLutAToBTagDataEntry"/> class.
        /// </summary>
        /// <param name="curveA">A Curve</param>
        /// <param name="clutValues">CLUT</param>
        /// <param name="curveM">M Curve</param>
        /// <param name="matrix3x3">Two dimensional conversion matrix (3x3)</param>
        /// <param name="matrix3x1">One dimensional conversion matrix (3x1)</param>
        /// <param name="curveB">B Curve</param>
        public IccLutAToBTagDataEntry(
            IccTagDataEntry[] curveB,
            float[,] matrix3x3,
            float[] matrix3x1,
            IccTagDataEntry[] curveM,
            IccClut clutValues,
            IccTagDataEntry[] curveA)
            : this(curveB, matrix3x3, matrix3x1, curveM, clutValues, curveA, IccProfileTag.Unknown)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IccLutAToBTagDataEntry"/> class.
        /// </summary>
        /// <param name="curveA">A Curve</param>
        /// <param name="clutValues">CLUT</param>
        /// <param name="curveM">M Curve</param>
        /// <param name="matrix3x3">Two dimensional conversion matrix (3x3)</param>
        /// <param name="matrix3x1">One dimensional conversion matrix (3x1)</param>
        /// <param name="curveB">B Curve</param>
        /// <param name="tagSignature">Tag Signature</param>
        public IccLutAToBTagDataEntry(
            IccTagDataEntry[] curveB,
            float[,] matrix3x3,
            float[] matrix3x1,
            IccTagDataEntry[] curveM,
            IccClut clutValues,
            IccTagDataEntry[] curveA,
            IccProfileTag tagSignature)
        : base(IccTypeSignature.LutAToB, tagSignature)
        {
            this.VerifyMatrix(matrix3x3, matrix3x1);
            this.VerifyCurve(curveA, nameof(curveA));
            this.VerifyCurve(curveB, nameof(curveB));
            this.VerifyCurve(curveM, nameof(curveM));

            this.Matrix3x3 = this.CreateMatrix3x3(matrix3x3);
            this.Matrix3x1 = this.CreateMatrix3x1(matrix3x1);
            this.CurveA = curveA;
            this.CurveB = curveB;
            this.CurveM = curveM;
            this.ClutValues = clutValues;

            if (this.IsAClutMMatrixB())
            {
                Guard.IsTrue(this.CurveB.Length == 3, nameof(this.CurveB), $"{nameof(this.CurveB)} must have a length of three");
                Guard.IsTrue(this.CurveM.Length == 3, nameof(this.CurveM), $"{nameof(this.CurveM)} must have a length of three");
                Guard.MustBeBetweenOrEqualTo(this.CurveA.Length, 1, 15, nameof(this.CurveA));

                this.InputChannelCount = curveA.Length;
                this.OutputChannelCount = 3;

                Guard.IsTrue(this.InputChannelCount == clutValues.InputChannelCount, nameof(clutValues), "Input channel count does not match the CLUT size");
                Guard.IsTrue(this.OutputChannelCount == clutValues.OutputChannelCount, nameof(clutValues), "Output channel count does not match the CLUT size");
            }
            else if (this.IsMMatrixB())
            {
                Guard.IsTrue(this.CurveB.Length == 3, nameof(this.CurveB), $"{nameof(this.CurveB)} must have a length of three");
                Guard.IsTrue(this.CurveM.Length == 3, nameof(this.CurveM), $"{nameof(this.CurveM)} must have a length of three");

                this.InputChannelCount = this.OutputChannelCount = 3;
            }
            else if (this.IsAClutB())
            {
                Guard.MustBeBetweenOrEqualTo(this.CurveA.Length, 1, 15, nameof(this.CurveA));
                Guard.MustBeBetweenOrEqualTo(this.CurveB.Length, 1, 15, nameof(this.CurveB));

                this.InputChannelCount = curveA.Length;
                this.OutputChannelCount = curveB.Length;

                Guard.IsTrue(this.InputChannelCount == clutValues.InputChannelCount, nameof(clutValues), "Input channel count does not match the CLUT size");
                Guard.IsTrue(this.OutputChannelCount == clutValues.OutputChannelCount, nameof(clutValues), "Output channel count does not match the CLUT size");
            }
            else if (this.IsB())
            {
                this.InputChannelCount = this.OutputChannelCount = this.CurveB.Length;
            }
            else
            {
                throw new ArgumentException("Invalid combination of values given");
            }
        }

        /// <summary>
        /// Gets the number of input channels
        /// </summary>
        public int InputChannelCount { get; }

        /// <summary>
        /// Gets the number of output channels
        /// </summary>
        public int OutputChannelCount { get; }

        /// <summary>
        /// Gets the two dimensional conversion matrix (3x3)
        /// </summary>
        public Matrix4x4? Matrix3x3 { get; }

        /// <summary>
        /// Gets the one dimensional conversion matrix (3x1)
        /// </summary>
        public Vector3? Matrix3x1 { get; }

        /// <summary>
        /// Gets the color lookup table
        /// </summary>
        public IccClut ClutValues { get; }

        /// <summary>
        /// Gets the B Curve
        /// </summary>
        public IccTagDataEntry[] CurveB { get; }

        /// <summary>
        /// Gets the M Curve
        /// </summary>
        public IccTagDataEntry[] CurveM { get; }

        /// <summary>
        /// Gets the A Curve
        /// </summary>
        public IccTagDataEntry[] CurveA { get; }

        /// <inheritdoc />
        public override bool Equals(IccTagDataEntry other)
        {
            if (base.Equals(other) && other is IccLutAToBTagDataEntry entry)
            {
                return this.InputChannelCount == entry.InputChannelCount
                    && this.OutputChannelCount == entry.OutputChannelCount
                    && this.Matrix3x1 == entry.Matrix3x1
                    && this.Matrix3x3 == entry.Matrix3x3
                    && this.ClutValues.Equals(entry.ClutValues)
                    && this.EqualsCurve(this.CurveA, entry.CurveA)
                    && this.EqualsCurve(this.CurveB, entry.CurveB)
                    && this.EqualsCurve(this.CurveM, entry.CurveM);
            }

            return false;
        }

        /// <inheritdoc />
        public bool Equals(IccLutAToBTagDataEntry other)
        {
            return this.Equals((IccTagDataEntry)other);
        }

        private bool EqualsCurve(IccTagDataEntry[] thisCurves, IccTagDataEntry[] entryCurves)
        {
            bool thisNull = thisCurves == null;
            bool entryNull = entryCurves == null;

            if (thisNull && entryNull)
            {
                return true;
            }

            if (entryNull)
            {
                return false;
            }

            return thisCurves.SequenceEqual(entryCurves);
        }

        private bool IsAClutMMatrixB()
        {
            return this.CurveB != null
                && this.Matrix3x3 != null
                && this.Matrix3x1 != null
                && this.CurveM != null
                && this.ClutValues != null
                && this.CurveA != null;
        }

        private bool IsMMatrixB()
        {
            return this.CurveB != null
                && this.Matrix3x3 != null
                && this.Matrix3x1 != null
                && this.CurveM != null;
        }

        private bool IsAClutB()
        {
            return this.CurveB != null
                && this.ClutValues != null
                && this.CurveA != null;
        }

        private bool IsB()
        {
            return this.CurveB != null;
        }

        private void VerifyCurve(IccTagDataEntry[] curves, string name)
        {
            if (curves != null)
            {
                bool isNotCurve = curves.Any(t => !(t is IccParametricCurveTagDataEntry) && !(t is IccCurveTagDataEntry));
                Guard.IsFalse(isNotCurve, nameof(name), $"{nameof(name)} must be of type {nameof(IccParametricCurveTagDataEntry)} or {nameof(IccCurveTagDataEntry)}");
            }
        }

        private void VerifyMatrix(float[,] matrix3x3, float[] matrix3x1)
        {
            if (matrix3x1 != null)
            {
                Guard.IsTrue(matrix3x1.Length == 3, nameof(matrix3x1), "Matrix must have a size of three");
            }

            if (matrix3x3 != null)
            {
                bool is3By3 = matrix3x3.GetLength(0) == 3 && matrix3x3.GetLength(1) == 3;
                Guard.IsTrue(is3By3, nameof(matrix3x3), "Matrix must have a size of three by three");
            }
        }

        private Vector3? CreateMatrix3x1(float[] matrix)
        {
            if (matrix == null)
            {
                return null;
            }

            return new Vector3(matrix[0], matrix[1], matrix[2]);
        }

        private Matrix4x4? CreateMatrix3x3(float[,] matrix)
        {
            if (matrix == null)
            {
                return null;
            }

            return new Matrix4x4(
                matrix[0, 0],
                matrix[0, 1],
                matrix[0, 2],
                0,
                matrix[1, 0],
                matrix[1, 1],
                matrix[1, 2],
                0,
                matrix[2, 0],
                matrix[2, 1],
                matrix[2, 2],
                0,
                0,
                0,
                0,
                1);
        }
    }
}
