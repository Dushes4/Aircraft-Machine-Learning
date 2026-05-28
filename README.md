# Aircraft Reinforcement Learning Module for Unity

This repository contains a modular aircraft control and training system for Unity based on reinforcement learning and the ML-Agents framework.

The project was developed as part of a graduation thesis focused on improving reinforcement learning control models for fixed-wing UAVs and aircraft simulation in Unity. The implementation extends an earlier prototype by introducing continuous controls, improved reward shaping, route tracking utilities, modular architecture, and support for both training and inference.

The system is designed for training AI agents to:

* follow generated routes,
* control aircraft in real-time physics simulation,
* perform takeoff and landing tasks,
* operate with continuous control inputs,
* use trained neural network models directly inside Unity.

The implementation and architecture are based strictly on the thesis description and the provided source code.
---

# Technologies

* Unity 6000.4.6f1
* C#
* Unity ML-Agents
* Python 3.9
* PPO and SAC reinforcement learning algorithms

Recommended ML-Agents related packages:

* `mlagents 0.30.0`
* `mlagents-envs 0.30.0`
* `torch 1.12.1`
* `torchvision 0.13.1`
* `protobuf 3.20.3`

---

# Project Structure

## Runtime Physics

### `AircraftPhysics`

Core aircraft physics simulation.

Responsible for:

* aerodynamic force calculation,
* aircraft movement,
* thrust application,
* fuel consumption,
* atmospheric and wind influence,
* interaction with aerodynamic surfaces.

The implementation is based on a fixed-wing aerodynamic model adapted for Unity.

### `AeroSurface`

Represents individual aerodynamic surfaces.

Each surface calculates aerodynamic forces independently and sends the result to `AircraftPhysics`.

### `AeroSurfaceConfig`

Stores aerodynamic surface parameters and configuration values.

### `BasicEngine`

Simple aircraft engine implementation used by the physics system.

### `GroundStateTracker`

Tracks aircraft ground contact state and landing-related conditions.

---

# Control System

## Common Control Components

### `AircraftInput`

Unified aircraft input structure used by controllers and agents.

### `AircraftActuator`

Applies control commands to the aircraft.

Responsible for:

* thrust,
* control surfaces,
* flaps,
* brakes.

Acts as an abstraction layer between control logic and physics.

### `AircraftActionDecoder`

Converts ML-Agent actions into valid aircraft control signals.

Supports:

* continuous actions,
* discrete actions,
* scaling and normalization of outputs.

---

# Reinforcement Learning Components

## `AircraftAgent`

Main ML-Agents training component.

Responsibilities:

* collecting observations,
* generating rewards,
* interacting with ML-Agents,
* route tracking,
* training logic.

The agent uses:

* route vectors,
* aircraft velocity,
* raycast sensor data,
* aircraft orientation information.

The reward system includes:

* checkpoint rewards,
* target alignment rewards,
* distance progress rewards,
* penalties for collisions,
* penalties for unstable movement.

The implementation supports:

* route following,
* takeoff training,
* landing training.

## `LearningController`

Controls training difficulty and curriculum learning.

The component dynamically changes training difficulty based on average reward statistics from recent episodes.

---

# Inference System

## `AircraftInferenceController`

Used for controlling aircraft with already trained neural network models.

Unlike `AircraftAgent`, this component does not perform training.

Responsibilities:

* loading trained models,
* switching between flight modes,
* applying model predictions,
* runtime AI aircraft control.

Supports separate models for:

* takeoff,
* flight,
* landing.

---

# Route System

## `Route`

Stores route data and checkpoint sequences.

## `Checkpoint`

Represents a single route checkpoint.

## `RouteTracker`

Tracks current route progress and active checkpoints.

The route system was designed for reinforcement learning scenarios with dynamically generated routes and checkpoint-based navigation.

---

# Editor Utilities

The repository also includes several Unity editor tools:

* `RouteEditor`
* `AircraftGizmos`
* `AeroSurfaceEditor`
* `AeroSurfaceConfigEditor`
* `AeroSurfaceConfigDrawer`
* `AircraftPhysicsDisplaySettings`

These utilities simplify:

* route visualization,
* aircraft debugging,
* aerodynamic configuration,
* editor workflow.

---

# Training Scenario

Typical training workflow:

1. Configure aircraft physics and aerodynamic surfaces.
2. Create or generate training routes.
3. Attach `AircraftAgent` to the aircraft.
4. Configure ML-Agents behavior parameters.
5. Start ML-Agents training from Python.
6. Train PPO or SAC models.
7. Export trained models.
8. Use `AircraftInferenceController` for runtime AI control.

The project supports multiple simultaneously trained agents using shared neural network models.

---

# ML-Agents Example

Example training command:

```bash
mlagents-learn aircraft.yaml --run-id=ppo_run
```

TensorBoard can be used for monitoring:

```bash
tensorboard --logdir results
```

---

# Main Improvements Compared to the Earlier Prototype

According to the thesis, the updated implementation introduces:

* continuous aircraft controls,
* modular architecture,
* improved reward shaping,
* curriculum learning improvements,
* better route tracking,
* additional metrics collection,
* smoother aircraft behavior,
* support for PPO and SAC,
* dedicated inference controller,
* improved observation system.

Experimental testing described in the thesis showed improvements in:

* flight smoothness,
* route following precision,
* training stability.

---

# Thesis Context

The project was created as part of a thesis dedicated to reinforcement learning for aircraft control in Unity.

The work focuses on:

* reinforcement learning for game AI,
* aircraft simulation,
* UAV control,
* PPO and SAC algorithms,
* modular Unity architecture,
* AI route navigation.

The thesis also includes:

* architecture analysis,
* reward system design,
* curriculum learning experiments,
* TensorBoard evaluation,
* comparison between old and new implementations.

---
