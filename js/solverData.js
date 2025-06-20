const Hit = require('./hit');

class SolverData {
  constructor(bpm, beatDivisions, beatTimings, hits) {
    this.bpm = bpm;
    this.beatDivisions = beatDivisions;
    this.beatTimings = beatTimings;
    this.hits = hits;
    this.nextVibes = new Array(hits.length);

    let nextVibe = hits.length;
    for (let i = hits.length - 1; i >= 0; i--) {
      if (hits[i].givesVibe) nextVibe = i;
      this.nextVibes[i] = nextVibe;
    }

    this.previousVibes = new Array(hits.length);
    let previousVibe = -1;
    for (let i = 0; i < hits.length; i++) {
      this.previousVibes[i] = previousVibe;
      if (hits[i].givesVibe) previousVibe = i;
    }
  }

  get hasBeatTimings() {
    return this.beatTimings.length > 1;
  }

  get hitCount() {
    return this.hits.length;
  }

  getBeatFromTime(time) {
    if (time === Infinity) return Infinity;
    if (time === -Infinity) return -Infinity;
    if (this.beatTimings.length <= 1) {
      return time / (60 / Math.max(1, this.bpm)) + 1;
    }
    let beatIndex = this.getBeatNumberFromTime(time);
    const previous = this.beatTimings[beatIndex];
    const next = this.beatTimings[beatIndex + 1];
    return beatIndex + 1 + (time - previous) / (next - previous);
  }

  getBeatNumberFromTime(time) {
    let min = 0;
    let max = this.beatTimings.length - 1;
    while (max >= min) {
      const mid = Math.floor((min + max) / 2);
      if (this.beatTimings[mid] > time) max = mid - 1; else min = mid + 1;
    }
    return Math.max(0, Math.min(max, this.beatTimings.length - 2));
  }

  getTimeFromBeat(beat) {
    if (beat === Infinity) return Infinity;
    if (beat === -Infinity) return -Infinity;
    if (this.beatTimings.length <= 1) {
      return (60 / Math.max(1, this.bpm)) * (beat - 1);
    }
    if (beat <= 1) {
      const first = this.beatTimings[0];
      const second = this.beatTimings[1];
      return first - (second - first) * (1 - beat);
    }
    if (beat < this.beatTimings.length) {
      const previous = this.beatTimings[Math.floor(beat) - 1];
      const next = this.beatTimings[Math.floor(beat)];
      return previous + (next - previous) * (beat % 1);
    }
    const last = this.beatTimings[this.beatTimings.length - 1];
    const secondToLast = this.beatTimings[this.beatTimings.length - 2];
    return last + (last - secondToLast) * (beat - this.beatTimings.length);
  }

  getTimeFromBeatInt(beat) {
    if (this.beatTimings.length <= 1) {
      return (60 / Math.max(1, this.bpm)) * (beat - 1);
    }
    if (beat < 1) {
      const first = this.beatTimings[0];
      const second = this.beatTimings[1];
      return first - (second - first) * (1 - beat);
    }
    if (beat <= this.beatTimings.length) {
      return this.beatTimings[beat - 1];
    }
    const last = this.beatTimings[this.beatTimings.length - 1];
    const secondToLast = this.beatTimings[this.beatTimings.length - 2];
    return last + (last - secondToLast) * (beat - this.beatTimings.length);
  }

  getBeatLengthAtTime(time) {
    if (this.beatTimings.length <= 1) return 60 / Math.max(1, this.bpm);
    const beatIndex = this.getBeatNumberFromTime(time);
    return this.beatTimings[beatIndex + 1] - this.beatTimings[beatIndex];
  }

  getBeatLengthForBeat(beat) {
    if (this.beatTimings.length <= 1) return 60 / Math.max(1, this.bpm);
    if (beat < 1) return this.beatTimings[1] - this.beatTimings[0];
    if (beat < this.beatTimings.length) return this.beatTimings[beat] - this.beatTimings[beat - 1];
    return this.beatTimings[this.beatTimings.length - 1] - this.beatTimings[this.beatTimings.length - 2];
  }

  getHitTime(hitIndex) {
    if (hitIndex < 0) return -Infinity;
    if (hitIndex >= this.hits.length) return Infinity;
    return this.hits[hitIndex].time;
  }

  getNextVibe(hitIndex) {
    if (hitIndex < 0) hitIndex = 0;
    return hitIndex < this.nextVibes.length ? this.nextVibes[hitIndex] : this.nextVibes.length;
  }

  getPreviousVibe(hitIndex) {
    if (hitIndex >= this.previousVibes.length) hitIndex = this.previousVibes.length - 1;
    return hitIndex >= 0 ? this.previousVibes[hitIndex] : -1;
  }

  getFirstHitIndexAfter(time) {
    let min = 0;
    let max = this.hits.length;
    while (max >= min && min < this.hits.length) {
      const mid = Math.floor((min + max) / 2);
      if (this.hits[mid].time > time) max = mid - 1; else min = mid + 1;
    }
    return min;
  }
}

module.exports = SolverData;
