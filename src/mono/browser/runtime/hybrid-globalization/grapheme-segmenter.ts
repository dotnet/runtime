// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This file is partially using code from FormatJS Intl.Segmenter implementation, reference: 
 * https://github.com/formatjs/formatjs/blob/58d6a7b398d776ca3d2726d72ae1573b65cc3bef/packages/intl-segmenter/src/segmenter.ts
 * https://github.com/formatjs/formatjs/blob/58d6a7b398d776ca3d2726d72ae1573b65cc3bef/packages/intl-segmenter/src/segmentation-utils.ts
 */

import { mono_assert } from "../globals";
import { isSurrogate } from "./helpers";

type SegmentationRule = {
    breaks: boolean
    before?: RegExp
    after?: RegExp
}

type SegmentationRuleRaw = {
    breaks: boolean
    before?: string
    after?: string
}
  
type SegmentationTypeRaw = {
    variables: Record<string, string>
    rules: Record<string, SegmentationRuleRaw>
}

let segmentationRules: Record<string, SegmentationRule>;

function replaceVariables(variables: Record<string, string>, input: string): string {
    const findVarRegex = /\$[A-Za-z0-9_]+/gm;
    return input.replaceAll(findVarRegex, match => {
        if (!(match in variables)) {
            throw new Error(`No such variable ${match}`);
        }
        return variables[match];
    });
}

function generateRegexRule (rule: string, variables: Record<string, string>, after: boolean): RegExp {
    return new RegExp(`${after ? "^" : ""}${replaceVariables(variables, rule)}${after ? "" : "$"}`);
}

function isSegmentationTypeRaw(obj: any): obj is SegmentationTypeRaw {
    return obj.variables != null && obj.rules != null;
}

export function setSegmentationRulesFromJson(json: string) {
    mono_assert(isSegmentationTypeRaw(json), "Provided grapheme segmentation rules are not valid");
    segmentationRules = GraphemeSegmenter.prepareSegmentationRules(json);
}

export class GraphemeSegmenter {
    private readonly rules: Record<string, SegmentationRule>;
    private readonly ruleSortedKeys: string[];

    public constructor() {  
        this.rules = segmentationRules;
        this.ruleSortedKeys = Object.keys(this.rules).sort((a, b) => Number(a) - Number(b));
    }

    /**
     * Returns the next grapheme in the given string starting from the specified index.
     * @param str - The input string.
     * @param startIndex - The starting index.
     * @returns The next grapheme.
     */
    public nextGrapheme(str: string, startIndex: number): string {
        const breakIdx = this.nextGraphemeBreak(str, startIndex);
        return str.substring(startIndex, breakIdx);
    }

    /**
     * Finds the index of the next grapheme break in a given string starting from a specified index.
     * 
     * @param str - The input string.
     * @param startIndex - The index to start searching from.
     * @returns The index of the next grapheme break.
     */
    public nextGraphemeBreak(str: string, startIndex: number): number {
        if (startIndex < 0)
            return 0;
    
        if (startIndex >= str.length - 1)
            return str.length;
    
        let prev = String.fromCodePoint(str.codePointAt(startIndex)!);
        for (let i = startIndex + 1; i < str.length; i++) {
            // Don't break surrogate pairs
            if (isSurrogate(str, i)) {
                continue;
            }

            const curr = String.fromCodePoint(str.codePointAt(i)!);
            if (this.isGraphemeBreak(prev, curr))
                return i;
    
            prev = curr;
        }
    
        return str.length;
    }

    private isGraphemeBreak(previous: string, current: string): boolean {
        for (const key of this.ruleSortedKeys) {
            const {before, after, breaks} = this.rules[key];
            // match before and after rules
            if (before && !before.test(previous)) {
                continue;
            }
            if (after && !after.test(current)) {
                continue;
            }

            return breaks;
        }

        // GB999: Any ÷ Any
        return true;
    }

    public static prepareSegmentationRules(segmentationRules: SegmentationTypeRaw): Record<string, SegmentationRule> {
        const preparedRules: Record<string, SegmentationRule> = {};
    
        for (const key of Object.keys(segmentationRules.rules)) {
            const ruleValue = segmentationRules.rules[key];
            const preparedRule: SegmentationRule = { breaks: ruleValue.breaks, };
    
            if ("before" in ruleValue && ruleValue.before) {
                preparedRule.before = generateRegexRule(ruleValue.before, segmentationRules.variables, false);
            }
            if ("after" in ruleValue && ruleValue.after) {
                preparedRule.after = generateRegexRule(ruleValue.after, segmentationRules.variables, true);
            }
    
            preparedRules[key] = preparedRule;
        }
        return preparedRules;
    }
}
