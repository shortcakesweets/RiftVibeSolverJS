class Hit {
  constructor(time, score, givesVibe) {
    this.time = time;
    this.score = score;
    this.givesVibe = givesVibe;
  }

  compare(other) {
    return this.time - other.time;
  }

  static mergeHits(hits) {
    const result = [];
    let currentTime = Number.NEGATIVE_INFINITY;
    let currentScore = 0;
    let currentGivesVibe = false;

    for (const hit of hits) {
      if (hit.time > currentTime) {
        if (currentScore > 0 || currentGivesVibe) {
          result.push(new Hit(currentTime, currentScore, currentGivesVibe));
        }
        currentTime = hit.time;
        currentScore = 0;
        currentGivesVibe = false;
      }
      currentScore += hit.score;
      currentGivesVibe = currentGivesVibe || hit.givesVibe;
    }

    if (currentScore > 0 || currentGivesVibe) {
      result.push(new Hit(currentTime, currentScore, currentGivesVibe));
    }
    return result;
  }
}

module.exports = Hit;
