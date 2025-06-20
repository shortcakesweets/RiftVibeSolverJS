class VibePath {
  constructor(startTime, endTime, startIndex, endIndex, score, segments) {
    this.startTime = startTime;
    this.endTime = endTime;
    this.startIndex = startIndex;
    this.endIndex = endIndex;
    this.score = score;
    this.segments = segments;
  }
}

module.exports = VibePath;
