﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2DESNet.PathMover
{
    public class Path
    {
        public int Id { get; private set; }
        public double Length { get; private set; }
        public double SpeedLimit { get; private set; }
        public Direction Direction { get; private set; }
        public List<ControlPoint> ControlPoints { get; private set; }

        internal Path(int id, double length, double maxSpeed, Direction direction)
        {
            Id = id;
            Length = length;
            SpeedLimit = maxSpeed;
            Direction = direction;
            ControlPoints = new List<ControlPoint>();
        }

        internal void Add(ControlPoint controlPoint, double position)
        {
            controlPoint.Positions.Add(this, position);
            ControlPoints.Add(controlPoint);
            if (controlPoint.Positions[this] < 0 || controlPoint.Positions[this] > Length)
                throw new Exceptions.InfeasibleConstruction(
                    "Control point must be positioned within the range of path length.");
            ControlPoints.Sort((t0, t1) => t0.Positions[this].CompareTo(t1.Positions[this]));
        }
        public double GetDistance(ControlPoint from, ControlPoint to)
        {
            if (from.Positions.ContainsKey(this) && to.Positions.ContainsKey(this))
            {
                double distance = to.Positions[this] - from.Positions[this];
                if (Direction == Direction.Backward) distance = -distance;
                else if (Direction == Direction.TwoWay) distance = Math.Abs(distance);
                if (distance >= 0) return distance;
            }
            return double.PositiveInfinity;
        }
    }

    public enum Direction { Forward, Backward, TwoWay }
}
