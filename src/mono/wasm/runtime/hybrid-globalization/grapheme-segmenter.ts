// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This file is partially using code from FormatJS Intl.Segmenter implementation, reference: 
 * https://github.com/formatjs/formatjs/blob/58d6a7b398d776ca3d2726d72ae1573b65cc3bef/packages/intl-segmenter/src/segmenter.ts
 * https://github.com/formatjs/formatjs/blob/58d6a7b398d776ca3d2726d72ae1573b65cc3bef/packages/intl-segmenter/src/segmentation-utils.ts
 */

import { SegmentationRules } from "./segmentation-rules";

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
  
type SegmentationTypeTypeRaw = {
    variables: Record<string, string>
    rules: Record<string, SegmentationRuleRaw>
}

function replace_variables(variables: Record<string, string>, input: string):string {
    const findVarRegex = /\$[A-Za-z0-9_]+/gm;
    return input.replaceAll(findVarRegex, match => {
        if (!(match in variables)) {
            throw new Error(`No such variable ${match}`);
        }
        return variables[match];
    });
}

function generate_rule_regex (rule: string, variables: Record<string, string>, after: boolean): RegExp {
    return new RegExp(`${after ? "^" : ""}${replace_variables(variables, rule)}${after ? "" : "$"}`);
}

function prepare_segmanation_rules(segmentationTypeValue: SegmentationTypeTypeRaw): Record<string, SegmentationRule> {
    const preparedRules: Record<string, SegmentationRule> = {};

    for (const ruleNr of Object.keys(segmentationTypeValue.rules)) {
        const ruleValue = segmentationTypeValue.rules[ruleNr];
        const preparedRule: SegmentationRule = {breaks: ruleValue.breaks,};

        if ("before" in ruleValue && ruleValue.before) {
            preparedRule.before = generate_rule_regex(ruleValue.before, segmentationTypeValue.variables, false);
        }
        if ("after" in ruleValue && ruleValue.after) {
            preparedRule.after = generate_rule_regex(ruleValue.after, segmentationTypeValue.variables, true);
        }

        preparedRules[ruleNr] = preparedRule;
    }
    return preparedRules;
}

export class GraphemeSegmenter {
    private readonly rules;
    private readonly ruleSortedKeys;

    public constructor() {  
        // Process segmentation rules
        this.rules = prepare_segmanation_rules(SegmentationRules);
        this.ruleSortedKeys = Object.keys(this.rules).sort((a, b) => Number(a) - Number(b));
    }


    public next_grapheme_break(str: string, startIndex: number): number {
        if (startIndex < 0)
            return 0;
    
        if (startIndex >= str.length - 1)
            return str.length;
    
        let prev = String.fromCodePoint(str.codePointAt(startIndex)!);
        for (let i = startIndex + 1; i < str.length; i++) {
            // check if we are in the middle of surrogate pair
            let high, low;
            if ((0xD800 <= (high = str.charCodeAt(i - 1)) && high <= 0xDBFF) &&
                (0xDC00 <= (low = str.charCodeAt(i)) && low <= 0xDFFF)) {
                continue;
            }
    
            const next = String.fromCodePoint(str.codePointAt(i)!);

            if (this.is_grapheme_break(prev, next))
                return i;
    
            prev = next;
        }
    
        return str.length;
    }

    private is_grapheme_break(prev: string, next: string): boolean {
        for (const key of this.ruleSortedKeys) {
            const {before, after, breaks} = this.rules[key];
            // match before and after rules
            if (before && !before.test(prev)) {
                continue;
            }
            if (after && !after.test(next)) {
                continue;
            }

            return breaks;
        }

        // GB999: Any ÷ Any
        return true;
    }
}
