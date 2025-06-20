const Activation = require('./activation');
const ActivationData = require('./activationData');
const ActivationSpan = require('./activationSpan');
const BestNextActivations = require('./bestNextActivations');
const SolverResult = require('./solverResult');
const VibePath = require('./vibePath');
const VibePathSegment = require('./vibePathSegment');

const VIBE_LENGTH = 5.0;

const Solver = {
  solve(data) {
    if (data.hitCount === 0) {
      return new SolverResult(
        0,
        [],
        [],
        [],
        []
      );
    }

    const singleVibeActivationData = getActivationData(data, 1);
    const doubleVibeActivationData = getActivationData(data, 2);
    const singleVibeActivations = singleVibeActivationData.map((_, i) => getActivationFromData(singleVibeActivationData, i, 1));
    const doubleVibeActivations = doubleVibeActivationData.map((_, i) => getActivationFromData(doubleVibeActivationData, i, 2));

    const { bestNextActivationsByFirstVibeIndex, totalScore } = getBestNextActivationsByFirstVibeIndex(
      data,
      singleVibeActivationData,
      doubleVibeActivationData
    );
    const { bestSingle, bestDouble } = getBestActivationIndices(
      data,
      singleVibeActivationData,
      doubleVibeActivationData,
      bestNextActivationsByFirstVibeIndex
    );

    const bestSingleVibeActivations = Array.from(bestSingle).map(i => singleVibeActivations[i]);
    const bestDoubleVibeActivations = Array.from(bestDouble).map(i => doubleVibeActivations[i]);

    return new SolverResult(
      totalScore,
      singleVibeActivations,
      doubleVibeActivations,
      bestSingleVibeActivations,
      bestDoubleVibeActivations
    );
  },

  getVibePath(data, startTime, vibesUsed) {
    const hits = data.hits;
    const span = getSpanStartingAt(data, startTime, data.getFirstHitIndexAfter(startTime), vibesUsed);
    const segments = [];
    let currentTime = span.startTime;
    let vibeRemaining = vibesUsed * VIBE_LENGTH;

    for (let i = span.startIndex; i < span.endIndex; i++) {
      const hit = hits[i];
      if (!hit.givesVibe) continue;
      segments.push(new VibePathSegment(currentTime, hit.time, vibeRemaining, Math.max(0, vibeRemaining - (hit.time - currentTime))));
      vibeRemaining = Math.max(
        VIBE_LENGTH,
        Math.min(vibeRemaining - (hit.time - currentTime) + VIBE_LENGTH, 2 * VIBE_LENGTH)
      );
      currentTime = hit.time;
    }

    segments.push(new VibePathSegment(currentTime, currentTime + vibeRemaining, vibeRemaining, 0));
    currentTime += vibeRemaining;

    const maxEndTime = data.getTimeFromBeat(data.getBeatFromTime(currentTime) + getVibeExtensionForBeatLength(data.getBeatLengthAtTime(currentTime), data.beatDivisions));
    const endTime = Math.min(maxEndTime, data.getHitTime(data.getFirstHitIndexAfter(currentTime)));

    let score = 0;
    for (let i = span.startIndex; i < span.endIndex; i++) score += hits[i].score;

    return new VibePath(startTime, endTime, span.startIndex, span.endIndex, score, segments);
  }
};

function getActivationData(data, vibesUsed) {
  const activations = [];
  let fromIndex = data.getNextVibe(0);
  if (vibesUsed === 2) fromIndex = data.getNextVibe(fromIndex + 1);
  if (fromIndex === data.hitCount) return activations;

  const hits = data.hits;
  const spans = [];

  for (let i = fromIndex; i < hits.length; i++) spans.push(getSpanStartingAfterHit(data, i, vibesUsed));
  for (let i = fromIndex; i < hits.length; i++) getSpansEndingOnHit(spans, data, i, vibesUsed);
  getSpansEndingOnBeats(spans, data, vibesUsed);

  if (spans.length === 0) return activations;

  spans.sort((a, b) => a.startTime - b.startTime);

  let currentStartIndex = spans[0].startIndex;
  let currentEndIndex = spans[0].startIndex;
  let score = 0;

  for (const span of spans) {
    if (span.startIndex <= fromIndex) continue;

    while (currentStartIndex < span.startIndex) {
      score -= hits[currentStartIndex].score;
      currentStartIndex++;
    }
    while (currentEndIndex < span.endIndex) {
      score += hits[currentEndIndex].score;
      currentEndIndex++;
    }
    while (currentEndIndex > span.endIndex) {
      currentEndIndex--;
      score -= hits[currentEndIndex].score;
    }

    if (activations.length > 0 && activations[activations.length - 1].minStartTime === span.startTime) {
      if (activations[activations.length - 1].endIndex >= span.endIndex) continue;
      activations.pop();
    }
    activations.push(new ActivationData(span.startTime, span.startIndex, span.endIndex, score));
  }
  return activations;
}

function getSpanStartingAt(data, startTime, firstHitIndex, vibesUsed) {
  let currentTime = startTime;
  let vibeRemaining = vibesUsed * VIBE_LENGTH;
  let vibeZeroTime = currentTime + vibeRemaining;
  let endTime = getEndTime();
  let nextVibeIndex = data.getNextVibe(firstHitIndex);

  while (nextVibeIndex < data.hitCount) {
    const nextVibeTime = data.getHitTime(nextVibeIndex);
    if (nextVibeTime > endTime) break;
    vibeRemaining = Math.max(
      VIBE_LENGTH,
      Math.min(vibeRemaining - (nextVibeTime - currentTime) + VIBE_LENGTH, 2 * VIBE_LENGTH)
    );
    currentTime = nextVibeTime;
    vibeZeroTime = currentTime + vibeRemaining;
    endTime = getEndTime();
    nextVibeIndex = data.getNextVibe(nextVibeIndex + 1);
  }

  return new ActivationSpan(startTime, firstHitIndex, data.getFirstHitIndexAfter(endTime));

  function getEndTime() {
    const maxEndTime = data.getTimeFromBeat(
      data.getBeatFromTime(vibeZeroTime) +
        getVibeExtensionForBeatLength(data.getBeatLengthAtTime(vibeZeroTime), data.beatDivisions)
    );
    return Math.min(maxEndTime, data.getHitTime(data.getFirstHitIndexAfter(vibeZeroTime)));
  }
}

function getSpanStartingAfterHit(data, hitIndex, vibesUsed) {
  return getSpanStartingAt(data, data.getHitTime(hitIndex), hitIndex + 1, vibesUsed);
}

function getSpansEndingOnHit(spans, data, hitIndex, vibesUsed) {
  const endTime = data.getHitTime(hitIndex);
  const endBeat = data.getBeatFromTime(endTime);
  const previousHitTime = data.getHitTime(hitIndex - 1);
  let endIndex;
  if (data.hits[hitIndex].givesVibe) endIndex = getSpanStartingAfterHit(data, hitIndex, 1).endIndex; else endIndex = hitIndex + 1;

  if (!data.hasBeatTimings) {
    let vibeZeroTime = data.getTimeFromBeat(endBeat - getVibeExtensionForBeatLength(data.getBeatLengthForBeat(Math.floor(endBeat)), data.beatDivisions));
    vibeZeroTime = Math.max(vibeZeroTime, previousHitTime);
    getSpansWhereVibeHitsZeroAt(spans, data, vibeZeroTime, endIndex, vibesUsed);
    return;
  }

  for (let i = Math.floor(endBeat); i >= 1; i--) {
    const beatTime = data.getTimeFromBeatInt(i);
    const nextBeatTime = data.getTimeFromBeatInt(i + 1);
    let vibeZeroTime = data.getTimeFromBeat(
      endBeat - getVibeExtensionForBeatLength(nextBeatTime - beatTime, data.beatDivisions)
    );
    vibeZeroTime = Math.max(vibeZeroTime, previousHitTime);
    if (vibeZeroTime > beatTime && vibeZeroTime < nextBeatTime) {
      getSpansWhereVibeHitsZeroAt(spans, data, vibeZeroTime, endIndex, vibesUsed);
    }
    if (beatTime <= previousHitTime) break;
  }
}

function getSpansEndingOnBeats(spans, data, vibesUsed) {
  const beatTimings = data.beatTimings;
  if (beatTimings.length <= 1) return;
  for (let i = 1; i < beatTimings.length - 1; i++) {
    const beatTime = beatTimings[i];
    const endTimeWithPreviousBeatLength = data.getTimeFromBeat(
      i + 1 + getVibeExtensionForBeatLength(beatTime - beatTimings[i - 1], data.beatDivisions)
    );
    const endTimeWithNextBeatLength = data.getTimeFromBeat(
      i + 1 + getVibeExtensionForBeatLength(beatTimings[i + 1] - beatTime, data.beatDivisions)
    );
    const nextHitIndex = data.getFirstHitIndexAfter(beatTime);
    const nextHitTime = data.getHitTime(nextHitIndex);
    if (!((endTimeWithPreviousBeatLength >= nextHitTime) ^ (endTimeWithNextBeatLength >= nextHitTime))) continue;
    let hitIndex = nextHitIndex;
    if (endTimeWithNextBeatLength < nextHitTime) hitIndex--;
    let endIndex;
    if (hitIndex < data.hitCount && data.hits[hitIndex].givesVibe) {
      endIndex = getSpanStartingAfterHit(data, hitIndex, 1).endIndex;
    } else {
      endIndex = hitIndex + 1;
    }
    getSpansWhereVibeHitsZeroAt(spans, data, beatTime, endIndex, vibesUsed);
  }
}

function getSpansWhereVibeHitsZeroAt(spans, data, vibeZeroTime, endIndex, vibesUsed) {
  let currentTime = vibeZeroTime;
  let vibeNeeded = 0;
  let previousVibeIndex = data.getPreviousVibe(data.getFirstHitIndexAfter(currentTime));

  do {
    const previousVibeTime = data.getHitTime(previousVibeIndex);
    const possibleStartTime = currentTime - (vibesUsed * VIBE_LENGTH - vibeNeeded);
    const needFullVibeAt = currentTime - (2 * VIBE_LENGTH - vibeNeeded);
    if (possibleStartTime > previousVibeTime) {
      spans.push(new ActivationSpan(possibleStartTime, data.getFirstHitIndexAfter(possibleStartTime), endIndex));
    }
    if (previousVibeTime < needFullVibeAt) break;
    vibeNeeded = Math.min(vibeNeeded + (currentTime - previousVibeTime) - VIBE_LENGTH, 2 * VIBE_LENGTH);
    if (vibeNeeded <= 0) break;
    currentTime = previousVibeTime;
    previousVibeIndex = data.getPreviousVibe(previousVibeIndex);
  } while (previousVibeIndex >= 0);
}

function getVibeExtensionForBeatLength(beatLength, beatDivisions) {
  const hitWindowInBeats = 0.175 / beatLength;
  return hitWindowInBeats - Math.floor(hitWindowInBeats * beatDivisions) / beatDivisions;
}

function getBestNextActivationsByFirstVibeIndex(data, singleVibeActivations, doubleVibeActivations) {
  const bestNextActivationsByFirstVibeIndex = new Map();

  const totalScore = getBestNextActivations(0).bestNextValue;
  return { bestNextActivationsByFirstVibeIndex, totalScore };

  function getBestNextActivations(fromIndex) {
    const firstVibeIndex = data.getNextVibe(fromIndex);
    if (bestNextActivationsByFirstVibeIndex.has(firstVibeIndex)) {
      return bestNextActivationsByFirstVibeIndex.get(firstVibeIndex);
    }
    const secondVibeIndex = data.getNextVibe(firstVibeIndex + 1);
    let bestNextValue = 0;
    const bestNextSingleVibeActivations = [];
    const bestNextDoubleVibeActivations = [];

    for (let i = 0; i < singleVibeActivations.length; i++) {
      const nextActivation = singleVibeActivations[i];
      if (nextActivation.startIndex <= firstVibeIndex) continue;
      if (nextActivation.startIndex > secondVibeIndex) break;
      const nextActivationValue = nextActivation.score + getBestNextActivations(nextActivation.endIndex).bestNextValue;
      if (nextActivationValue > bestNextValue) {
        bestNextValue = nextActivationValue;
        bestNextSingleVibeActivations.length = 0;
      }
      if (nextActivationValue === bestNextValue) {
        bestNextSingleVibeActivations.push(i);
      }
    }

    for (let i = 0; i < doubleVibeActivations.length; i++) {
      const nextActivation = doubleVibeActivations[i];
      if (nextActivation.startIndex <= secondVibeIndex) continue;
      const nextActivationValue = nextActivation.score + getBestNextActivations(nextActivation.endIndex).bestNextValue;
      if (nextActivationValue > bestNextValue) {
        bestNextValue = nextActivationValue;
        bestNextSingleVibeActivations.length = 0;
        bestNextDoubleVibeActivations.length = 0;
      }
      if (nextActivationValue === bestNextValue) {
        bestNextDoubleVibeActivations.push(i);
      }
    }

    const best = new BestNextActivations(bestNextValue, bestNextSingleVibeActivations, bestNextDoubleVibeActivations);
    bestNextActivationsByFirstVibeIndex.set(firstVibeIndex, best);
    return best;
  }
}

function getBestActivationIndices(data, singleVibeActivationData, doubleVibeActivationData, bestNextActivationsByFirstVibeIndex) {
  const bestSingle = new Set();
  const bestDouble = new Set();
  traverse(0);
  return { bestSingle, bestDouble };

  function traverse(fromIndex) {
    const bestNext = bestNextActivationsByFirstVibeIndex.get(data.getNextVibe(fromIndex));
    for (const index of bestNext.bestNextSingleVibeActivations) {
      if (!bestSingle.has(index)) {
        bestSingle.add(index);
        traverse(singleVibeActivationData[index].endIndex);
      }
    }
    for (const index of bestNext.bestNextDoubleVibeActivations) {
      if (!bestDouble.has(index)) {
        bestDouble.add(index);
        traverse(doubleVibeActivationData[index].endIndex);
      }
    }
  }
}

function getActivationFromData(activationData, index, vibesUsed) {
  const activation = activationData[index];
  return new Activation(
    activation.minStartTime,
    index < activationData.length - 1 ? activationData[index + 1].minStartTime : activation.minStartTime + 1,
    activation.score,
    vibesUsed
  );
}

module.exports = Solver;
