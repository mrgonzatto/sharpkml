﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace SharpKml.Engine
{
    using System;
    using SharpKml.Base;
    using SharpKml.Dom;

    /// <summary>
    /// Provides extension methods for <see cref="Feature"/> objects.
    /// </summary>
    public static class FeatureExtensions
    {
        // This is used in CalculateLookAt to give a margin around the feature.
        private const double Margin = 1.1;

        // To avoid zooming in too far to point features or features that are
        // spatially small, we clap the computed range to a minimum value.
        private const double MinimumRange = 1000.0; // Meters

        // The range of the LookAt that emcompasses the feature's extents
        // depends on the field of view of the virtual camera.
        private static readonly double FieldOfView = MathHelpers.DegreesToRadians(60.0);

        /// <summary>
        /// Calculates the coordinates of the bounds of the <see cref="Feature"/>.
        /// </summary>
        /// <param name="feature">The feature instance.</param>
        /// <returns>
        /// A <c>BoundingBox</c> containing the coordinates of the bounds of the
        /// feature.
        /// </returns>
        /// <remarks>
        /// If the feature is a <see cref="Container"/> then the returned value
        /// will be the bounds of all the features within that container.
        /// </remarks>
        /// <exception cref="ArgumentNullException">feature is null.</exception>
        public static BoundingBox CalculateBounds(this Feature feature)
        {
            Check.IsNotNull(feature, nameof(feature));

            var box = new BoundingBox();
            ExpandBox(feature, box);

            return box.IsEmpty ? null : box;
        }

        /// <summary>
        /// Calculates a <see cref="LookAt"/> object from the spatial extents
        /// of the <see cref="Feature"/>.
        /// </summary>
        /// <param name="feature">The feature instance.</param>
        /// <returns>
        /// A <c>LookAt</c> object with the calculated data (see remarks) on
        /// success; otherwise, null if the bounds of the <c>Feature</c> could
        /// not be calculated.
        /// </returns>
        /// <remarks>
        /// <para>The Altitude, Heading and Tilt values or the returned
        /// <c>LookAt</c> are not set. AltitudeMode is set to RelativeToGroud.
        /// </para><para>The range is computed such that the feature will be
        /// within a viewport with a field of view of 60 deg and is clamped to
        /// a minimum of 1,000 meters.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">feature is null.</exception>
        public static LookAt CalculateLookAt(this Feature feature)
        {
            BoundingBox box = CalculateBounds(feature); // Will throw if feature is null
            if (box != null)
            {
                return ComputeLookAt(box);
            }

            return null;
        }

        private static LookAt ComputeLookAt(BoundingBox box)
        {
            Vector center = box.Center;
            double north = MathHelpers.Distance(box.Center, new Vector(box.North, center.Longitude));
            double west = MathHelpers.Distance(box.Center, new Vector(center.Latitude, box.West));

            // Calculate the corner and range
            double northWest = Math.Sqrt(Math.Pow(north, 2) + Math.Pow(west, 2));
            double range = northWest * Math.Tan(FieldOfView) * Margin;

            return new LookAt
            {
                AltitudeMode = AltitudeMode.RelativeToGround,
                Latitude = center.Latitude,
                Longitude = center.Longitude,
                Range = Math.Max(range, MinimumRange), // Clamp value
            };
        }

        private static void ExpandBox(Feature feature, BoundingBox box)
        {
            if (feature is Placemark placemark)
            {
                GeometryExtensions.ExpandBox(placemark.Geometry, box); // Can pass in nulls
                return;
            }

            if (feature is Container container)
            {
                foreach (Feature f in container.Features)
                {
                    ExpandBox(f, box);
                }

                return;
            }

            // It's not a placemark or container, try it as a IBoundsInformation
            // and allow the conversion to fail
            GeometryExtensions.ExpandBox(feature as IBoundsInformation, box);
        }
    }
}
